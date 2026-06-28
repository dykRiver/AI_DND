import globalAxios, { AxiosResponse, AxiosInstance, AxiosRequestConfig } from 'axios';
import { Configuration } from '../configuration';
// Some imports not used depending on template conditions
// @ts-ignore
import { BASE_PATH, COLLECTION_FORMATS, RequestArgs, BaseAPI, RequiredError } from '../base';
import { AdminResultInt64 } from '../models';
import { DatabaseSyncInput } from '../models/database-sync-input';
import { DeleteDatabaseSyncInput } from '../models/delete-dbsync-input';
import {AdminResultListDatabaseSync} from  '../models/admin-result-list-dbsync';
/**
 * DatabaseSyncApi - axios parameter creator
 * @export
 */
export const DatabaseSyncApiAxiosParamCreator = function (configuration?: Configuration) {
    return {
        /**
         * 
         * @summary 增加表同步信息
         * @param {DatabaseSyncInput} [body] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        apiDatabaseSyncAddPost: async (body?: DatabaseSyncInput, options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/dataSyncManager/add`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, 'https://example.com');
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }
            const localVarRequestOptions :AxiosRequestConfig = { method: 'POST', ...baseOptions, ...options};
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

            localVarHeaderParameter['Content-Type'] = 'application/json-patch+json';

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
            const needsSerialization = (typeof body !== "string") || localVarRequestOptions.headers['Content-Type'] === 'application/json';
            localVarRequestOptions.data =  needsSerialization ? JSON.stringify(body !== undefined ? body : {}) : (body || "");

            return {
                url: localVarUrlObj.pathname + localVarUrlObj.search + localVarUrlObj.hash,
                options: localVarRequestOptions,
            };
        },
        /**
         * 
         * @summary 删除表同步信息
         * @param {DeleteDatabaseSyncInput} [body] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        apiDatabaseSyncDeletePost: async (body?: DeleteDatabaseSyncInput, options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/dataSyncManager/delete`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, 'https://example.com');
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }
            const localVarRequestOptions :AxiosRequestConfig = { method: 'POST', ...baseOptions, ...options};
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

            localVarHeaderParameter['Content-Type'] = 'application/json-patch+json';

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
            const needsSerialization = (typeof body !== "string") || localVarRequestOptions.headers['Content-Type'] === 'application/json';
            localVarRequestOptions.data =  needsSerialization ? JSON.stringify(body !== undefined ? body : {}) : (body || "");

            return {
                url: localVarUrlObj.pathname + localVarUrlObj.search + localVarUrlObj.hash,
                options: localVarRequestOptions,
            };
        },
        /**
         * 
         * @summary 获取表同步信息列表
         * @param {number} id 主键Id
         * @param {string} [name] 名称
         * @param {string} [code] 编码
         * @param {string} [type] 表同步信息类型
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        apiDatabaseSyncListGet: async (id: number, name?: string, code?: string, type?: string, options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            // verify required parameter 'id' is not null or undefined
            if (id === null || id === undefined) {
                throw new RequiredError('id','Required parameter id was null or undefined when calling apiDatabaseSyncListGet.');
            }
            const localVarPath = `/api/dataSyncManager/list`;
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

            if (name !== undefined) {
                localVarQueryParameter['Name'] = name;
            }

            if (code !== undefined) {
                localVarQueryParameter['Code'] = code;
            }

            if (type !== undefined) {
                localVarQueryParameter['Type'] = type;
            }

            if (id !== undefined) {
                localVarQueryParameter['Id'] = id;
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
         * @summary 更新表同步信息
         * @param {DatabaseSyncInput} [body] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        apiDatabaseSyncUpdatePost: async (body?: DatabaseSyncInput, options: AxiosRequestConfig = {}): Promise<RequestArgs> => {
            const localVarPath = `/api/dataSyncManager/update`;
            // use dummy base URL string because the URL constructor only accepts absolute URLs.
            const localVarUrlObj = new URL(localVarPath, 'https://example.com');
            let baseOptions;
            if (configuration) {
                baseOptions = configuration.baseOptions;
            }
            const localVarRequestOptions :AxiosRequestConfig = { method: 'POST', ...baseOptions, ...options};
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

            localVarHeaderParameter['Content-Type'] = 'application/json-patch+json';

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
            const needsSerialization = (typeof body !== "string") || localVarRequestOptions.headers['Content-Type'] === 'application/json';
            localVarRequestOptions.data =  needsSerialization ? JSON.stringify(body !== undefined ? body : {}) : (body || "");

            return {
                url: localVarUrlObj.pathname + localVarUrlObj.search + localVarUrlObj.hash,
                options: localVarRequestOptions,
            };
        },
    }
};

/**
 * DatabaseSyncApi - functional programming interface
 * @export
 */
export const DatabaseSyncApiFp = function(configuration?: Configuration) {
    return {
        /**
         * 
         * @summary 增加表同步信息
         * @param {DatabaseSyncInput} [body] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiDatabaseSyncAddPost(body?: DatabaseSyncInput, options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<AdminResultInt64>>> {
            const localVarAxiosArgs = await DatabaseSyncApiAxiosParamCreator(configuration).apiDatabaseSyncAddPost(body, options);
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
        /**
         * 
         * @summary 删除表同步信息
         * @param {DeleteDatabaseSyncInput} [body] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiDatabaseSyncDeletePost(body?: DeleteDatabaseSyncInput, options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<void>>> {
            const localVarAxiosArgs = await DatabaseSyncApiAxiosParamCreator(configuration).apiDatabaseSyncDeletePost(body, options);
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
        /**
         * 
         * @summary 获取表同步信息列表
         * @param {number} id 主键Id
         * @param {string} [name] 名称
         * @param {string} [code] 编码
         * @param {string} [type] 表同步信息类型
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiDatabaseSyncListGet(id: number, name?: string, code?: string, type?: string, options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<AdminResultListDatabaseSync>>> {
            const localVarAxiosArgs = await DatabaseSyncApiAxiosParamCreator(configuration).apiDatabaseSyncListGet(id, name, code, type, options);
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
        /**
         * 
         * @summary 更新表同步信息
         * @param {DatabaseSyncInput} [body] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiDatabaseSyncUpdatePost(body?: DatabaseSyncInput, options?: AxiosRequestConfig): Promise<(axios?: AxiosInstance, basePath?: string) => Promise<AxiosResponse<void>>> {
            const localVarAxiosArgs = await DatabaseSyncApiAxiosParamCreator(configuration).apiDatabaseSyncUpdatePost(body, options);
            return (axios: AxiosInstance = globalAxios, basePath: string = BASE_PATH) => {
                const axiosRequestArgs :AxiosRequestConfig = {...localVarAxiosArgs.options, url: basePath + localVarAxiosArgs.url};
                return axios.request(axiosRequestArgs);
            };
        },
    }
};

/**
 * DatabaseSyncApi - factory interface
 * @export
 */
export const DatabaseSyncApiFactory = function (configuration?: Configuration, basePath?: string, axios?: AxiosInstance) {
    return {
        /**
         * 
         * @summary 增加表同步信息
         * @param {DatabaseSyncInput} [body] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiDatabaseSyncAddPost(body?: DatabaseSyncInput, options?: AxiosRequestConfig): Promise<AxiosResponse<AdminResultInt64>> {
            return DatabaseSyncApiFp(configuration).apiDatabaseSyncAddPost(body, options).then((request) => request(axios, basePath));
        },
        /**
         * 
         * @summary 删除表同步信息
         * @param {DeleteDatabaseSyncInput} [body] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiDatabaseSyncDeletePost(body?: DeleteDatabaseSyncInput, options?: AxiosRequestConfig): Promise<AxiosResponse<void>> {
            return DatabaseSyncApiFp(configuration).apiDatabaseSyncDeletePost(body, options).then((request) => request(axios, basePath));
        },
        /**
         * 
         * @summary 获取表同步信息列表
         * @param {number} id 主键Id
         * @param {string} [name] 名称
         * @param {string} [code] 编码
         * @param {string} [type] 表同步信息类型
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiDatabaseSyncListGet(id: number, name?: string, code?: string, type?: string, options?: AxiosRequestConfig): Promise<AxiosResponse<AdminResultListDatabaseSync>> {
            return DatabaseSyncApiFp(configuration).apiDatabaseSyncListGet(id, name, code, type, options).then((request) => request(axios, basePath));
        },
        /**
         * 
         * @summary 更新表同步信息
         * @param {DatabaseSyncInput} [body] 
         * @param {*} [options] Override http request option.
         * @throws {RequiredError}
         */
        async apiDatabaseSyncUpdatePost(body?: DatabaseSyncInput, options?: AxiosRequestConfig): Promise<AxiosResponse<void>> {
            return DatabaseSyncApiFp(configuration).apiDatabaseSyncUpdatePost(body, options).then((request) => request(axios, basePath));
        },
    };
};

/**
 * DatabaseSyncApi - object-oriented interface
 * @export
 * @class DatabaseSyncApi
 * @extends {BaseAPI}
 */
export class DatabaseSyncApi extends BaseAPI {
    /**
     * 
     * @summary 增加表同步信息
     * @param {DatabaseSyncInput} [body] 
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof DatabaseSyncApi
     */
    public async apiDatabaseSyncAddPost(body?: DatabaseSyncInput, options?: AxiosRequestConfig) : Promise<AxiosResponse<AdminResultInt64>> {
        return DatabaseSyncApiFp(this.configuration).apiDatabaseSyncAddPost(body, options).then((request) => request(this.axios, this.basePath));
    }
    /**
     * 
     * @summary 删除表同步信息
     * @param {DeleteDatabaseSyncInput} [body] 
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof DatabaseSyncApi
     */
    public async apiDatabaseSyncDeletePost(body?: DeleteDatabaseSyncInput, options?: AxiosRequestConfig) : Promise<AxiosResponse<void>> {
        return DatabaseSyncApiFp(this.configuration).apiDatabaseSyncDeletePost(body, options).then((request) => request(this.axios, this.basePath));
    }
    /**
     * 
     * @summary 获取表同步信息列表
     * @param {number} id 主键Id
     * @param {string} [name] 名称
     * @param {string} [code] 编码
     * @param {string} [type] 表同步信息类型
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof DatabaseSyncApi
     */
    public async apiDatabaseSyncListGet(id: number, name?: string, code?: string, type?: string, options?: AxiosRequestConfig) : Promise<AxiosResponse<AdminResultListDatabaseSync>> {
        return DatabaseSyncApiFp(this.configuration).apiDatabaseSyncListGet(id, name, code, type, options).then((request) => request(this.axios, this.basePath));
    }
    /**
     * 
     * @summary 更新表同步信息
     * @param {DatabaseSyncInput} [body] 
     * @param {*} [options] Override http request option.
     * @throws {RequiredError}
     * @memberof DatabaseSyncApi
     */
    public async apiDatabaseSyncUpdatePost(body?: DatabaseSyncInput, options?: AxiosRequestConfig) : Promise<AxiosResponse<void>> {
        return DatabaseSyncApiFp(this.configuration).apiDatabaseSyncUpdatePost(body, options).then((request) => request(this.axios, this.basePath));
    }
}