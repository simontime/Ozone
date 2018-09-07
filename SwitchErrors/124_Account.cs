using System.Linq;

namespace Ozone.SwitchErrors
{
    internal class _124_Account
    {
        public static string[] Lookup(string Error)
        {
            string Detail;
            string Reason;

            var ErrRange400X = new string[] { "4000", "4001", "4002", "4003", "4004", "4005", "4006", "4008" };
            var ErrRange402X = new string[] { "4021", "4022", "4023", "4026" };

            if (ErrRange400X.Contains(Error))
            {
                Detail = "Generic console-wide online services restriction.";
                Reason = "Unknown.";
                goto Return;
            }
            else if (ErrRange402X.Contains(Error))
            {
                Detail = "Generic title, gamecard or account-related online services restriction.";
                Reason = "Unknown";
                goto Return;
            }
            switch (Error)
            {
                case "4007":

                    Detail = "Your console has been banned from accessing online services.";
                    Reason = "Your console's unique client certificate was banned by Nintendo.";
                    break;

                case "4020":

                    Detail = "The Nintendo account linked to this console has been banned.";
                    Reason = "There are multiple reasons this could happen.\n\n1. Purchased a \"download code\" from one of the websites that require you to log in to a throwaway account to download the game.\n2. Cheating/hacking on online games.\n3. Fraudulent credit card info entered.\n\nEssentially, you broke the Nintendo Accounts & Online Services EULA.";
                    break;

                case "4024":

                    Detail = "This download title is unauthorized to connect to its online services.";
                    Reason = "This download title's digital certificate is invalid, and application authorization services refuses to return a JWT (JSON web token) to authorize communication to the game's servers for online play.";
                    break;

                case "4025":

                    Detail = "This cartridge title is unauthorized to connect to its online services.";
                    Reason = "This cartridge's digital certificate is invalid, and application authorization services refuses to return a JWT (JSON web token) to authorize communication to the game's servers for online play.";
                    break;

                case "4027":

                    Detail = "Your console has been banned from accessing this title's online services.";
                    Reason = "Cheating/hacking or transmitting inappropriate material while using this title's online services.";
                    break;

                case "4028":

                    Detail = "Online services for this title are not available at this time.";
                    Reason = "The servers used to host online services for this title are under maintenance, only available at certain times or all player slots are full.";
                    break;

                case "4029":

                    Detail = "Online services for this title have been permanently discontinued.";
                    Reason = "The servers used to host online services for this title are no longer active.";
                    break;

                default:

                    Detail = "Sorry.";
                    Reason = "Error code not in database.";
                    break;
            }

            Return:
            return new string[] { Detail, Reason };
        }
    }
}