import globalAxios, { AxiosResponse, AxiosInstance, AxiosRequestConfig } from 'axios';
import { Configuration } from '../configuration';
// Some imports not used depending on template conditions
// @ts-ignore
import { BASE_PATH, COLLECTION_FORMATS, RequestArgs, BaseAPI, RequiredError } from '../base';
/**
 * Home - axios parameter creator
 * @export
 */
export const MainPageApiAxiosParamCreator = function (configuration?: Configuration) {
    return {
        /**
         * 
         * @summary 获取处方状态统计信息
         * @throws {RequiredError}
         */
        prescriptionInfoStatisticsListGet: async (options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/prescriptionInfo/prescriptionInfoStatistics`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, 'https://example.com');
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }
            const localVarRequestOptions :AxiosRequestConfig = { method: 'GET', ...baseOptions, ...options};
            const localVarHeaderParameter = {} as any;
            const localVarQueryParameter = {} as any;

            // authentication Bearer required
            // http bearer authentication required
            if (configuration && configuration.accessToken) {
                const accessToken = typeof configuration.accessToken === 'function'
                    ? await configuration.accessToken()
                    : await configuration.accessToken;
                localVarHeaderParameter["Authorization"] = "Bearer " + accessToken;
            }

            const query = new URLSearchParams(localVarUrlObj.search);
            for (const key in localVarQueryParameter) {
                query.set(key, localVarQueryParameter[key]);
            }
            for (const key in options.params) {
                query.set(key, options.params[key]);
            }
            localVarUrlObj.search = (new URLSearchParams(query)).toString();
            let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
            localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};

            return {
                url: localVarUrlObj.pathname + localVarUrlObj.search + localVarUrlObj.hash,
                options: localVarRequestOptions,
            };
        },

         /**
         * 
         * @summary 产能统计信息
         * @throws {RequiredError}
         */
        capacityStatisticsListGet: async (options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
                    const localVarPath = `/api/prescriptionInfo/capacityStatistics`;
                    // use dummy base URL string because the URL constructor only accepts absolute URLs.
                    const localVarUrlObj = new URL(localVarPath, 'https://example.com');
                    let baseOptions;
                    if (configuration) {
                        baseOptions = configuration.baseOptions;
                    }
                    const localVarRequestOptions :AxiosRequestConfig = { method: 'GET', ...baseOptions, ...options};
                    const localVarHeaderParameter = {} as any;
                    const localVarQueryParameter = {} as any;
        
                    // authentication Bearer required
                    // http bearer authentication required
                    if (configuration && configuration.accessToken) {
                        const accessToken = typeof configuration.accessToken === 'function'
                            ? await configuration.accessToken()
                            : await configuration.accessToken;
                        localVarHeaderParameter["Authorization"] = "Bearer " + accessToken;
                    }
        
                    const query = new URLSearchParams(localVarUrlObj.search);
                    for (const key in localVarQueryParameter) {
                        query.set(key, localVarQueryParameter[key]);
                    }
                    for (const key in options.params) {
                        query.set(key, options.params[key]);
                    }
                    localVarUrlObj.search = (new URLSearchParams(query)).toString();
                    let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
                    localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};
        
                    return {
                        url: localVarUrlObj.pathname + localVarUrlObj.search + localVarUrlObj.hash,
                        options: localVarRequestOptions,
                    };
        },

        splitInfoGet: async (options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/prescriptionInfo/getSplitPatientNames`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, 'https://example.com');
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }
            const localVarRequestOptions :AxiosRequestConfig = { method: 'Post', ...baseOptions, ...options};
            const localVarHeaderParameter = {} as any;
            const localVarQueryParameter = {} as any;

            // authentication Bearer required
            // http bearer authentication required
            if (configuration && configuration.accessToken) {
                const accessToken = typeof configuration.accessToken === 'function'
                    ? await configuration.accessToken()
                    : await configuration.accessToken;
                localVarHeaderParameter["Authorization"] = "Bearer " + accessToken;
            }

            const query = new URLSearchParams(localVarUrlObj.search);
            for (const key in localVarQueryParameter) {
                query.set(key, localVarQueryParameter[key]);
            }
            for (const key in options.params) {
                query.set(key, options.params[key]);
            }
            localVarUrlObj.search = (new URLSearchParams(query)).toString();
            let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
            localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};

            return {
                url: localVarUrlObj.pathname + localVarUrlObj.search + localVarUrlObj.hash,
                options: localVarRequestOptions,
            };
        },

        decoctionInfoGet: async (options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/decoctionDevice/getDecoctionEnableInfo`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, 'https://example.com');
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }
            const localVarRequestOptions :AxiosRequestConfig = { method: 'Post', ...baseOptions, ...options};
            const localVarHeaderParameter = {} as any;
            const localVarQueryParameter = {} as any;

            // authentication Bearer required
            // http bearer authentication required
            if (configuration && configuration.accessToken) {
                const accessToken = typeof configuration.accessToken === 'function'
                    ? await configuration.accessToken()
                    : await configuration.accessToken;
                localVarHeaderParameter["Authorization"] = "Bearer " + accessToken;
            }

            const query = new URLSearchParams(localVarUrlObj.search);
            for (const key in localVarQueryParameter) {
                query.set(key, localVarQueryParameter[key]);
            }
            for (const key in options.params) {
                query.set(key, options.params[key]);
            }
            localVarUrlObj.search = (new URLSearchParams(query)).toString();
            let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
            localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};

            return {
                url: localVarUrlObj.pathname + localVarUrlObj.search + localVarUrlObj.hash,
                options: localVarRequestOptions,
            };
        },

        packingInfoGet: async (options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/packingDevice/getPackingEnableInfo`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, 'https://example.com');
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }
            const localVarRequestOptions :AxiosRequestConfig = { method: 'Post', ...baseOptions, ...options};
            const localVarHeaderParameter = {} as any;
            const localVarQueryParameter = {} as any;

            // authentication Bearer required
            // http bearer authentication required
            if (configuration && configuration.accessToken) {
                const accessToken = typeof configuration.accessToken === 'function'
                    ? await configuration.accessToken()
                    : await configuration.accessToken;
                localVarHeaderParameter["Authorization"] = "Bearer " + accessToken;
            }

            const query = new URLSearchParams(localVarUrlObj.search);
            for (const key in localVarQueryParameter) {
                query.set(key, localVarQueryParameter[key]);
            }
            for (const key in options.params) {
                query.set(key, options.params[key]);
            }
            localVarUrlObj.search = (new URLSearchParams(query)).toString();
            let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
            localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};

            return {
                url: localVarUrlObj.pathname + localVarUrlObj.search + localVarUrlObj.hash,
                options: localVarRequestOptions,
            };
        },

        statisticsByHospitalAndDeliveryMethodGet: async (options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/prescriptionInfo/statisticsByHospitalAndDeliveryMethod`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, 'https://example.com');
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }
            const localVarRequestOptions :AxiosRequestConfig = { method: 'Get', ...baseOptions, ...options};
            const localVarHeaderParameter = {} as any;
            const localVarQueryParameter = {} as any;

            // authentication Bearer required
            // http bearer authentication required
            if (configuration && configuration.accessToken) {
                const accessToken = typeof configuration.accessToken === 'function'
                    ? await configuration.accessToken()
                    : await configuration.accessToken;
                localVarHeaderParameter["Authorization"] = "Bearer " + accessToken;
            }

            const query = new URLSearchParams(localVarUrlObj.search);
            for (const key in localVarQueryParameter) {
                query.set(key, localVarQueryParameter[key]);
            }
            for (const key in options.params) {
                query.set(key, options.params[key]);
            }
            localVarUrlObj.search = (new URLSearchParams(query)).toString();
            let headersFromBaseOptions = baseOptions && baseOptions.headers ? baseOptions.headers : {};
            localVarRequestOptions.headers = {...localVarHeaderParameter, ...headersFromBaseOptions, ...options.headers};

            return {
                url: localVarUrlObj.pathname + localVarUrlObj.search + localVarUrlObj.hash,
                options: localVarRequestOptions,
            };
        },
    }
};

/**
 * Home - functional programming interface
 * @export
 */
export const PrescriptionApiFp = function(configuration?: Configuration) {
    return {
        /**
         * 
         * @summary 获取处方状态统计信息
         * @throws {RequiredError}
         */
        async prescriptionInfoStatisticsListGet(options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<any>>> {
            const localVarAxiosArgs = await MainPageApiAxiosParamCreator(configuration).prescriptionInfoStatisticsListGet();
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
        async capacityStatisticsListGet(options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<any>>> {
            const localVarAxiosArgs = await MainPageApiAxiosParamCreator(configuration).capacityStatisticsListGet();
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
        async splitInfoGet(options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<any>>> {
            const localVarAxiosArgs = await MainPageApiAxiosParamCreator(configuration).splitInfoGet();
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
        async decoctionInfoGet(options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<any>>> {
            const localVarAxiosArgs = await MainPageApiAxiosParamCreator(configuration).decoctionInfoGet();
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
        async packingInfoGet(options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<any>>> {
            const localVarAxiosArgs = await MainPageApiAxiosParamCreator(configuration).packingInfoGet();
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
        async statisticsByHospitalAndDeliveryMethodGet(options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<any>>> {
            const localVarAxiosArgs = await MainPageApiAxiosParamCreator(configuration).statisticsByHospitalAndDeliveryMethodGet();
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
    }
};

/**
 * Home - factory interface
 * @export
 */
export const PresctiptionApiFactory = function (configuration?: Configuration, basePath?: string, axios?: AxiosInstance) {
    return {
        /**
         * 
         * @summary 获取处方状态统计信息
         * @throws {RequiredError}
         */
        async apiDatabaseSyncListGet(options?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
            return PrescriptionApiFp(configuration).prescriptionInfoStatisticsListGet(options).then((request) => request(axios, basePath));
        },
        async capacityStatisticsListGet(options?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
            return PrescriptionApiFp(configuration).capacityStatisticsListGet(options).then((request) => request(axios, basePath));
        },
        async splitInfoGet(options?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
            return PrescriptionApiFp(configuration).splitInfoGet(options).then((request) => request(axios, basePath));
        },
        async decoctionInfoGet(options?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
            return PrescriptionApiFp(configuration).decoctionInfoGet(options).then((request) => request(axios, basePath));
        },
        async packingInfoGet(options?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
            return PrescriptionApiFp(configuration).packingInfoGet(options).then((request) => request(axios, basePath));
        },
        async statisticsByHospitalAndDeliveryMethodGet(options?: AxiosRequestConfig): Promise<AxiosResponse<any>> {
            return PrescriptionApiFp(configuration).statisticsByHospitalAndDeliveryMethodGet(options).then((request) => request(axios, basePath));
        }
    };
};

/**
 * Home - object-oriented interface
 * @export
 * @class DatabaseSyncApi
 * @extends {BaseAPI}
 */
export class HomeInfoApi extends BaseAPI {
    /**
     * 
     * @summary 获取处方状态统计信息
     * @throws {RequiredError}
     * @memberof DatabaseSyncApi
     */
    public async apiDatabaseSyncListGet(options?: AxiosRequestConfig) : Promise<AxiosResponse<any>> {
        return PrescriptionApiFp(this.configuration).prescriptionInfoStatisticsListGet(options).then((request) => request(this.axios, this.basePath));
    }
    public async capacityStatisticsListGet(options?: AxiosRequestConfig) : Promise<AxiosResponse<any>> {
        return PrescriptionApiFp(this.configuration).capacityStatisticsListGet(options).then((request) => request(this.axios, this.basePath));
    }
    public async splitInfoGet(options?: AxiosRequestConfig) : Promise<AxiosResponse<any>> {
        return PrescriptionApiFp(this.configuration).splitInfoGet(options).then((request) => request(this.axios, this.basePath));
    }
    public async decoctionInfoGet(options?: AxiosRequestConfig) : Promise<AxiosResponse<any>> {
        return PrescriptionApiFp(this.configuration).decoctionInfoGet(options).then((request) => request(this.axios, this.basePath));
    }
    public async packingInfoGet(options?: AxiosRequestConfig) : Promise<AxiosResponse<any>> {
        return PrescriptionApiFp(this.configuration).packingInfoGet(options).then((request) => request(this.axios, this.basePath));
    }
    public async statisticsByHospitalAndDeliveryMethodGet(options?: AxiosRequestConfig) : Promise<AxiosResponse<any>> {
        return PrescriptionApiFp(this.configuration).statisticsByHospitalAndDeliveryMethodGet(options).then((request) => request(this.axios, this.basePath));
    }
}