namespace ASC.Web.Solution.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
