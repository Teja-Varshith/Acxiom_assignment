using System.Threading.Tasks;

namespace axciom.Services
{
    public interface IAuditLogger
    {
        Task LogAsync(string userId, string userName, string action, string entityName, string recordId = "", string oldValue = "", string newValue = "", string result = "Success", string details = "", string ipAddress = "");
    }
}
