/**
* 数据同步配置表
*
* @export
* @interface DatabaseSync
*/
export interface DatabaseSync {

    /**
     * 雪花Id
     *
     * @type {number}
     * @memberof SysOrg
     */
    id?: number;

    /**
     * 创建时间
     *
     * @type {Date}
     * @memberof DatabaseSync
     */
    createTime?: Date | null;

    /**
     * 更新时间
     *
     * @type {Date}
     * @memberof DatabaseSync
     */
    updateTime?: Date | null;

    /**
     * 创建者Id
     *
     * @type {number}
     * @memberof DatabaseSync
     */
    createUserId?: number | null;

    /**
     * 创建者姓名
     *
     * @type {string}
     * @memberof DatabaseSync
     */
    createUserName?: string | null;

    /**
     * 修改者Id
     *
     * @type {number}
     * @memberof DatabaseSync
     */
    updateUserId?: number | null;

    /**
     * 修改者姓名
     *
     * @type {string}
     * @memberof DatabaseSync
     */
    updateUserName?: string | null;

    /**
     * 软删除
     *
     * @type {boolean}
     * @memberof DatabaseSync
     */
    isDelete?: boolean;

    /**
     * 租户Id
     *
     * @type {number}
     * @memberof DatabaseSync
     */
    tenantId?: number | null;

    /**
     * 表名
     *
     * @type {string}
     * @memberof DatabaseSync
     */
    tableName: string;

    /**
    * 最后一次的同步时间
    *
    * @type {string}
    * @memberof DatabaseSync
    */
    dastSynTime?: Date | null;

    /**
    * 最后一次全量同步时间
    *
    * @type {string}
    * @memberof DatabaseSync
    */
    lastFullTableSynTime?: Date | null;

    /**
    * 初始化时间
    *
    * @type {string}
    * @memberof DatabaseSync
    */
    initSynTime?: Date | null;

    /**
     * 表结构变化同步时间
     *
     * @type {string}
     * @memberof DatabaseSync
     */
    structUpdateTime?: Date | null;

    /**
     * @type {boolean}
     * @memberof DatabaseSync
     */
    enable?: boolean;

    /**
     * 备注
     *
     * @type {string}
     * @memberof DatabaseSync
     */
    remark?: string | null;

    /**
     * 排序
     *
     * @type {number}
     * @memberof DatabaseSync
     */
    orderNo: number;
}