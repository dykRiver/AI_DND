namespace DHY.Game.Core.Services;

/// <summary>
/// 背包管理服务（重量制 + 装备槽位）
/// </summary>
[ApiDescriptionSettings("Game")]
public class InventoryService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameInventoryItem> _itemRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;

    public InventoryService(
        SqlSugarRepository<GameInventoryItem> itemRep,
        SqlSugarRepository<GameCharacter> characterRep)
    {
        _itemRep = itemRep;
        _characterRep = characterRep;
    }

    /// <summary>
    /// 添加道具（校验重量上限）
    /// </summary>
    [DisplayName("添加道具")]
    [HttpPost("addItem")]
    public async Task<GameInventoryItem> AddItemAsync([FromBody] AddItemInput input)
    {
        var character = await _characterRep.GetFirstAsync(c => c.Id == input.CharacterId)
            ?? throw Oops.Oh("角色不存在");

        // 计算当前总重量
        var currentWeight = await GetCurrentWeightAsync(input.CharacterId);

        // 关键道具不占重量，其他道具校验重量上限
        if (!input.IsKeyItem && input.Weight > 0)
        {
            var newTotalWeight = currentWeight + input.Weight * input.Quantity;
            if (newTotalWeight > character.WeightCapacity)
                throw Oops.Oh($"背包超重！当前{currentWeight}/{character.WeightCapacity}，无法再装入{input.Weight * input.Quantity}单位");
        }

        // 检查是否已有同名道具（可堆叠，仅限消耗品和杂物）
        if (input.ItemType is "消耗品" or "杂物" && !input.IsKeyItem)
        {
            var existing = await _itemRep.GetFirstAsync(i =>
                i.CharacterId == input.CharacterId && i.ItemName == input.ItemName);
            if (existing != null)
            {
                existing.Quantity += input.Quantity;
                if (!input.IsUnlimited && input.MaxUses > 0)
                    existing.CurrentUses += input.MaxUses; // 堆叠补充使用次数（如子弹）
                await _itemRep.AsUpdateable(existing)
                    .UpdateColumns(i => new { i.Quantity, i.CurrentUses })
                    .ExecuteCommandAsync();
                return existing;
            }
        }

        var item = new GameInventoryItem
        {
            CharacterId = input.CharacterId,
            ItemName = input.ItemName,
            ItemType = input.ItemType,
            Description = input.Description,
            Quantity = input.Quantity,
            IsEquipped = false,
            IsKeyItem = input.IsKeyItem,
            Properties = input.Properties,
            Weight = input.IsKeyItem ? 0 : input.Weight,
            AttributeBonus = input.AttributeBonus,
            LinkedAttribute = input.LinkedAttribute,
            MaxUses = input.MaxUses,
            CurrentUses = input.IsUnlimited ? 0 : input.MaxUses,
            IsUnlimited = input.IsUnlimited
        };

        await _itemRep.AsInsertable(item).ExecuteCommandAsync();
        return item;
    }

    /// <summary>
    /// 装备道具（1武器+1防具，新装备自动替换旧装备）
    /// </summary>
    [DisplayName("装备道具")]
    [HttpPost("equipItem")]
    public async Task<EquipResult> EquipItemAsync([FromBody] CharacterItemInput input)
    {
        var characterId = await ResolveCharacterIdAsync(input.CharacterId, input.SessionId);
        var item = await _itemRep.GetFirstAsync(i => i.Id == input.ItemId && i.CharacterId == characterId)
            ?? throw Oops.Oh("道具不存在");

        if (item.ItemType != "武器" && item.ItemType != "防具")
            throw Oops.Oh("该道具类型不可装备");

        var character = await _characterRep.GetFirstAsync(c => c.Id == characterId)
            ?? throw Oops.Oh("角色不存在");

        GameInventoryItem? unequipped = null;

        if (item.ItemType == "武器")
        {
            // 卸掉旧武器
            if (character.EquippedWeaponId.HasValue && character.EquippedWeaponId != item.Id)
            {
                var oldWeapon = await _itemRep.GetFirstAsync(i => i.Id == character.EquippedWeaponId);
                if (oldWeapon != null)
                {
                    oldWeapon.IsEquipped = false;
                    await _itemRep.AsUpdateable(oldWeapon).UpdateColumns(i => new { i.IsEquipped }).ExecuteCommandAsync();
                    unequipped = oldWeapon;
                }
            }
            character.EquippedWeaponId = item.Id;
        }
        else // 防具
        {
            if (character.EquippedArmorId.HasValue && character.EquippedArmorId != item.Id)
            {
                var oldArmor = await _itemRep.GetFirstAsync(i => i.Id == character.EquippedArmorId);
                if (oldArmor != null)
                {
                    oldArmor.IsEquipped = false;
                    await _itemRep.AsUpdateable(oldArmor).UpdateColumns(i => new { i.IsEquipped }).ExecuteCommandAsync();
                    unequipped = oldArmor;
                }
            }
            character.EquippedArmorId = item.Id;
        }

        item.IsEquipped = true;
        await _itemRep.AsUpdateable(item).UpdateColumns(i => new { i.IsEquipped }).ExecuteCommandAsync();
        await _characterRep.AsUpdateable(character)
            .UpdateColumns(c => new { c.EquippedWeaponId, c.EquippedArmorId })
            .ExecuteCommandAsync();

        return new EquipResult
        {
            EquippedItem = item,
            UnequippedItem = unequipped
        };
    }

    /// <summary>
    /// 卸装道具
    /// </summary>
    [DisplayName("卸装道具")]
    [HttpPost("unequipItem")]
    public async Task<GameInventoryItem> UnequipItemAsync([FromBody] CharacterItemInput input)
    {
        var characterId = await ResolveCharacterIdAsync(input.CharacterId, input.SessionId);
        var item = await _itemRep.GetFirstAsync(i => i.Id == input.ItemId && i.CharacterId == characterId)
            ?? throw Oops.Oh("道具不存在");

        if (!item.IsEquipped)
            throw Oops.Oh("该道具当前未装备");

        var character = await _characterRep.GetFirstAsync(c => c.Id == characterId)
            ?? throw Oops.Oh("角色不存在");

        item.IsEquipped = false;
        await _itemRep.AsUpdateable(item).UpdateColumns(i => new { i.IsEquipped }).ExecuteCommandAsync();

        if (item.ItemType == "武器" && character.EquippedWeaponId == item.Id)
        {
            character.EquippedWeaponId = null;
            await _characterRep.AsUpdateable(character).UpdateColumns(c => new { c.EquippedWeaponId }).ExecuteCommandAsync();
        }
        else if (item.ItemType == "防具" && character.EquippedArmorId == item.Id)
        {
            character.EquippedArmorId = null;
            await _characterRep.AsUpdateable(character).UpdateColumns(c => new { c.EquippedArmorId }).ExecuteCommandAsync();
        }

        return item;
    }

    /// <summary>
    /// 丢弃道具（减轻重量，关键道具不可丢弃）
    /// </summary>
    [DisplayName("丢弃道具")]
    [HttpPost("dropItem")]
    public async Task DropItemAsync([FromBody] DropItemInput input)
    {
        var characterId = await ResolveCharacterIdAsync(input.CharacterId, input.SessionId);
        var item = await _itemRep.GetFirstAsync(i => i.Id == input.ItemId && i.CharacterId == characterId)
            ?? throw Oops.Oh("道具不存在");

        if (item.IsKeyItem)
            throw Oops.Oh("关键道具不可丢弃");

        // 若已装备，先卸装
        if (item.IsEquipped)
        {
            var character = await _characterRep.GetFirstAsync(c => c.Id == characterId);
            if (character != null)
            {
                if (item.ItemType == "武器" && character.EquippedWeaponId == item.Id)
                {
                    character.EquippedWeaponId = null;
                    await _characterRep.AsUpdateable(character).UpdateColumns(c => new { c.EquippedWeaponId }).ExecuteCommandAsync();
                }
                else if (item.ItemType == "防具" && character.EquippedArmorId == item.Id)
                {
                    character.EquippedArmorId = null;
                    await _characterRep.AsUpdateable(character).UpdateColumns(c => new { c.EquippedArmorId }).ExecuteCommandAsync();
                }
            }
        }

        if (item.Quantity <= input.Quantity)
        {
            await _itemRep.DeleteByIdAsync(item.Id);
        }
        else
        {
            item.Quantity -= input.Quantity;
            await _itemRep.AsUpdateable(item)
                .UpdateColumns(i => new { i.Quantity })
                .ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 获取背包状态（重量+道具列表+装备信息）
    /// </summary>
    [DisplayName("获取背包状态")]
    [HttpGet("getBackpack")]
    public async Task<BackpackStatus> GetBackpackAsync([FromQuery] SessionIdInput input)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == input.SessionId)
            ?? throw Oops.Oh("角色不存在");

        var items = await _itemRep.AsQueryable()
            .Where(i => i.CharacterId == character.Id)
            .OrderByDescending(i => i.IsEquipped)
            .OrderBy(i => i.ItemType)
            .ToListAsync();

        var currentWeight = items.Where(i => !i.IsKeyItem).Sum(i => i.Weight * i.Quantity);
        var maxWeight = character.WeightCapacity;
        var weightPercent = maxWeight > 0 ? (double)currentWeight / maxWeight * 100 : 0;

        var equippedWeapon = items.FirstOrDefault(i => i.Id == character.EquippedWeaponId);
        var equippedArmor = items.FirstOrDefault(i => i.Id == character.EquippedArmorId);

        return new BackpackStatus
        {
            Items = items,
            CurrentWeight = currentWeight,
            MaxWeight = maxWeight,
            WeightPercent = Math.Round(weightPercent, 1),
            IsOverloaded = weightPercent >= 100,
            IsEncumbered = weightPercent >= 70,
            EquippedWeapon = equippedWeapon,
            EquippedArmor = equippedArmor
        };
    }

    /// <summary>
    /// 校验重量状态（供Hub层调用，阻断超重行动）
    /// </summary>
    public async Task<WeightCheckResult> CheckWeightAsync(long sessionId)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        if (character == null)
            return new WeightCheckResult { IsBlocked = false };

        var currentWeight = await GetCurrentWeightAsync(character.Id);
        var maxWeight = character.WeightCapacity;
        var ratio = maxWeight > 0 ? (double)currentWeight / maxWeight : 0;

        return new WeightCheckResult
        {
            CurrentWeight = currentWeight,
            MaxWeight = maxWeight,
            WeightRatio = ratio,
            IsBlocked = ratio >= 1.0,  // 100% 阻断
            IsEncumbered = ratio >= 0.7 // 70% 轻度超载
        };
    }

    /// <summary>
    /// 获取当前装备提供的属性加值（供JudgmentService调用）
    /// </summary>
    internal async Task<Dictionary<string, int>> GetEquipmentBonusesAsync(long characterId)
    {
        var bonuses = new Dictionary<string, int>();
        var character = await _characterRep.GetFirstAsync(c => c.Id == characterId);
        if (character == null) return bonuses;

        // 武器加值
        if (character.EquippedWeaponId.HasValue)
        {
            var weapon = await _itemRep.GetFirstAsync(i => i.Id == character.EquippedWeaponId);
            if (weapon != null && weapon.IsEquipped &&
                !string.IsNullOrEmpty(weapon.LinkedAttribute) &&
                (weapon.IsUnlimited || weapon.CurrentUses > 0))
            {
                var attr = weapon.LinkedAttribute.ToUpper();
                bonuses[attr] = bonuses.GetValueOrDefault(attr) + weapon.AttributeBonus;
            }
        }

        // 防具加值
        if (character.EquippedArmorId.HasValue)
        {
            var armor = await _itemRep.GetFirstAsync(i => i.Id == character.EquippedArmorId);
            if (armor != null && armor.IsEquipped &&
                !string.IsNullOrEmpty(armor.LinkedAttribute) &&
                (armor.IsUnlimited || armor.CurrentUses > 0))
            {
                var attr = armor.LinkedAttribute.ToUpper();
                bonuses[attr] = bonuses.GetValueOrDefault(attr) + armor.AttributeBonus;
            }
        }

        return bonuses;
    }

    /// <summary>
    /// 扣除已装备武器和防具各1次使用次数（战斗判定后调用）
    /// </summary>
    internal async Task DeductEquipmentUsesAsync(long characterId)
    {
        var character = await _characterRep.GetFirstAsync(c => c.Id == characterId);
        if (character == null) return;

        // 扣武器使用次数
        if (character.EquippedWeaponId.HasValue)
        {
            var weapon = await _itemRep.GetFirstAsync(i => i.Id == character.EquippedWeaponId);
            if (weapon != null && !weapon.IsUnlimited && weapon.CurrentUses > 0)
            {
                weapon.CurrentUses--;
                await _itemRep.AsUpdateable(weapon)
                    .UpdateColumns(i => new { i.CurrentUses })
                    .ExecuteCommandAsync();
            }
        }

        // 扣防具使用次数
        if (character.EquippedArmorId.HasValue)
        {
            var armor = await _itemRep.GetFirstAsync(i => i.Id == character.EquippedArmorId);
            if (armor != null && !armor.IsUnlimited && armor.CurrentUses > 0)
            {
                armor.CurrentUses--;
                await _itemRep.AsUpdateable(armor)
                    .UpdateColumns(i => new { i.CurrentUses })
                    .ExecuteCommandAsync();
            }
        }
    }

    /// <summary>
    /// 按道具名称精确扣减背包道具数量（导演AI声明消耗时调用）
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="itemName">道具名称（精确匹配）</param>
    /// <param name="quantity">消耗数量</param>
    /// <returns>是否成功扣减（false=背包中未找到该道具）</returns>
    internal async Task<bool> ConsumeItemByNameAsync(long characterId, string itemName, int quantity)
    {
        var item = await _itemRep.AsQueryable()
            .Where(i => i.CharacterId == characterId
                        && i.ItemName == itemName
                        && !i.IsEquipped
                        && !i.IsKeyItem
                        && !i.IsUnlimited)
            .FirstAsync();

        if (item == null) return false;

        if (item.Quantity <= quantity)
        {
            await _itemRep.DeleteByIdAsync(item.Id);
        }
        else
        {
            item.Quantity -= quantity;
            await _itemRep.AsUpdateable(item)
                .UpdateColumns(i => new { i.Quantity })
                .ExecuteCommandAsync();
        }

        return true;
    }

    /// <summary>
    /// 获取当前背包总重量（内部调用）
    /// </summary>
    internal async Task<decimal> GetCurrentWeightAsync(long characterId)
    {
        var items = await _itemRep.AsQueryable()
            .Where(i => i.CharacterId == characterId && !i.IsKeyItem)
            .ToListAsync();
        return items.Sum(i => i.Weight * i.Quantity);
    }

    /// <summary>
    /// 解析角色ID（优先使用CharacterId，为0时通过SessionId查找）
    /// </summary>
    private async Task<long> ResolveCharacterIdAsync(long characterId, long sessionId)
    {
        if (characterId > 0) return characterId;
        if (sessionId > 0)
        {
            var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
            if (character != null) return character.Id;
        }
        throw Oops.Oh("无法确定角色：请提供角色ID或会话ID");
    }
}

// ========== DTO ==========

/// <summary>
/// 添加道具输入
/// </summary>
public class AddItemInput
{
    /// <summary>角色ID</summary>
    public long CharacterId { get; set; }
    /// <summary>道具名称</summary>
    public string ItemName { get; set; } = "";
    /// <summary>道具类型 (武器/防具/消耗品/关键道具/杂物)</summary>
    public string ItemType { get; set; } = "杂物";
    /// <summary>描述</summary>
    public string? Description { get; set; }
    /// <summary>数量</summary>
    public int Quantity { get; set; } = 1;
    /// <summary>是否关键道具</summary>
    public bool IsKeyItem { get; set; }
    /// <summary>道具属性JSON（遗留兼容）</summary>
    public string? Properties { get; set; }
    /// <summary>重量单位（支持1位小数）</summary>
    public decimal Weight { get; set; }
    /// <summary>属性加值</summary>
    public int AttributeBonus { get; set; }
    /// <summary>关联属性 (STR/DEX/CON/INT/WIS/CHA)</summary>
    public string? LinkedAttribute { get; set; }
    /// <summary>最大使用次数（0=无限）</summary>
    public int MaxUses { get; set; }
    /// <summary>是否无限使用</summary>
    public bool IsUnlimited { get; set; }
}

/// <summary>
/// 丢弃道具输入
/// </summary>
public class DropItemInput
{
    /// <summary>角色ID（优先使用）</summary>
    public long CharacterId { get; set; }
    /// <summary>会话ID（CharacterId=0时按此查找角色）</summary>
    public long SessionId { get; set; }
    /// <summary>道具ID</summary>
    public long ItemId { get; set; }
    /// <summary>数量</summary>
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// 角色道具操作输入
/// </summary>
public class CharacterItemInput
{
    /// <summary>角色ID（优先使用）</summary>
    public long CharacterId { get; set; }
    /// <summary>会话ID（CharacterId=0时按此查找角色）</summary>
    public long SessionId { get; set; }
    /// <summary>道具ID</summary>
    public long ItemId { get; set; }
}

/// <summary>
/// 装备结果
/// </summary>
public class EquipResult
{
    /// <summary>新装备的道具</summary>
    public GameInventoryItem EquippedItem { get; set; } = null!;
    /// <summary>被替换卸下的道具（null表示没有旧装备）</summary>
    public GameInventoryItem? UnequippedItem { get; set; }
}

/// <summary>
/// 背包状态（GET接口返回）
/// </summary>
public class BackpackStatus
{
    /// <summary>道具列表</summary>
    public List<GameInventoryItem> Items { get; set; } = new();
    /// <summary>当前总重量</summary>
    public decimal CurrentWeight { get; set; }
    /// <summary>容量上限</summary>
    public int MaxWeight { get; set; }
    /// <summary>重量百分比</summary>
    public double WeightPercent { get; set; }
    /// <summary>是否超重（>=100%，阻断行动）</summary>
    public bool IsOverloaded { get; set; }
    /// <summary>是否负重（>=70%，DEX检定-2）</summary>
    public bool IsEncumbered { get; set; }
    /// <summary>已装备武器</summary>
    public GameInventoryItem? EquippedWeapon { get; set; }
    /// <summary>已装备防具</summary>
    public GameInventoryItem? EquippedArmor { get; set; }
}

/// <summary>
/// 重量校验结果（内部调用）
/// </summary>
public class WeightCheckResult
{
    /// <summary>当前重量</summary>
    public decimal CurrentWeight { get; set; }
    /// <summary>容量上限</summary>
    public int MaxWeight { get; set; }
    /// <summary>重量比（0-1）</summary>
    public double WeightRatio { get; set; }
    /// <summary>是否阻断（>=100%）</summary>
    public bool IsBlocked { get; set; }
    /// <summary>是否负重（>=70%）</summary>
    public bool IsEncumbered { get; set; }
}
