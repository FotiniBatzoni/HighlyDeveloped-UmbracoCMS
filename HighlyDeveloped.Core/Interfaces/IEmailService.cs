using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HighlyDeveloped.Core.Interfaces
{
    /// <summary>
    /// Handle all email operations
    /// </summary>
    public interface IEmailService
    {
        void SendContactNotificationToAdmin(ContactFormViewModel vm);

        void SendVerifyEmailAddressNotification(string membersEmail, string verificationToken);
    }
}
