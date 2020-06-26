namespace RabbitMQHelper.Models
{
    public class RabbitMessageResponseModel
    {
        public bool HandledSuccessfully { get; set; } = true;
        public bool ServerThrownError { get; set; } = false;
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
