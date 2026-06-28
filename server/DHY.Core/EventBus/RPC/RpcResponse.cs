namespace DHY.Core;

public class RpcResponse<T>
{
    /// <summary>
    /// 1= 成功
    /// </summary>
    public int Code { get; set; }
    public string Msg { get; set; }
    public T Data { get; set; }
    public T GetJsonResultData() => Data.DeserializeJsonResult<T>();

    public bool IsSuccess => Code == 1;
    public static RpcResponse<T> SuccessResponse(T data) => new RpcResponse<T> { Code = 1, Msg = "ok", Data = data };
    public static RpcResponse<T> FailResponse => new RpcResponse<T> { Code = 0, Msg = "fail" };
}                          

/// <summary>
/// RPC事件总线通用响应
/// </summary>
public class RpcResponse : RpcResponse<object>
{
    public static  RpcResponse OfflineResponse => new RpcResponse { Code = -1, Msg = "offline" };
    public static new RpcResponse SuccessResponse => new RpcResponse { Code = 1, Msg = "ok" };
    public static new RpcResponse FailResponse => new RpcResponse { Code = 0, Msg = "fail" };
    public static RpcResponse TimeoutResponse => new RpcResponse { Code = 400, Msg = "time out" };
    public static RpcResponse NotExistsResponse => new RpcResponse { Code = 1000, Msg = "not exists" };
    public static RpcResponse InvalidResponse => new RpcResponse { Code = 1, Msg = "invalid operation" };
}
