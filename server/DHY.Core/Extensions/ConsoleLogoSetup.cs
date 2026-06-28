using Furion.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DHY.Core;

/// <summary>
/// 控制台logo
/// </summary>
[SuppressSniffer]
public static class ConsoleLogoSetup
{
    public static void AddConsoleLogo(this IServiceCollection services)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(@"
     ______   ____  ____  ____  ____  
    |_   _ `.|_   ||   _||_  _||_  _| 
      | | `. \ | |__| |    \ \  / /   
      | |  | | |  __  |     \ \/ /    
     _| |_.' /_| |  | |_    _|  |_    
    |______.'|____||____|  |______|   
                                      ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(@"项目地址: http://192.168.80.50/ddcs/ddcsmain");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("东华原全自动煎药中心");
        Console.WriteLine();
    }
}