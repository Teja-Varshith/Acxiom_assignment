using axciom.Data;
using axciom.Models;
using System;
using System.Threading.Tasks;

namespace axciom.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly ApplicationDbContext _context;

        public AuditLogger(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string userName, string action, string entityName, string recordId = "", string oldValue = "", string newValue = "", string result = "Success", string details = "", string ipAddress = "")
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId ?? "Anonymous",
                    UserName = userName ?? "System",
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId ?? "",
                    OldValue = oldValue ?? "",
                    NewValue = newValue ?? "",
                    Timestamp = DateTime.UtcNow,
                    Result = result,
                    Details = details ?? "",
                    IpAddress = ipAddress ?? ""
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Prevent audit failures from crashing primary business requests
            }
        }
    }
}
