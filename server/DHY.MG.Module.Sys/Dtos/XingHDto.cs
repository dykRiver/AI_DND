namespace DHY.MG.Module.Sys.Dtos
{
    public class XingHDto
    {
    }

    //构造请求体
    public class XH_JsonRequest
    {
        public XH_Header header { get; set; }
        public XH_Parameter parameter { get; set; }
        public XH_Payload payload { get; set; }
    }

    public class XH_Header
    {
        public string app_id { get; set; }
        public string uid { get; set; }
    }

    public class XH_Parameter
    {
        public XH_Chat chat { get; set; }
    }

    public class XH_Chat
    {
        public string domain { get; set; }
        public double temperature { get; set; }
        public int max_tokens { get; set; }
    }

    public class XH_Payload
    {
        public XH_Message message { get; set; }
    }

    public class XH_Message
    {
        public List<XH_Content> text { get; set; }
    }

    public class XH_Content
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
