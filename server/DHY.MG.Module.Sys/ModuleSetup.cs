namespace DHY.MG.Module.Sys
{

    public static class ModuleSetup
    {
        public static IServiceCollection AddDevices(this IServiceCollection services)
        {
            //注意：请勿简化命名空间，因为与common有重名类

            return services;
        }


        public static IServiceCollection AddEventbusHandlers(this IServiceCollection services)
        {

            return services;
        }


    }
}
