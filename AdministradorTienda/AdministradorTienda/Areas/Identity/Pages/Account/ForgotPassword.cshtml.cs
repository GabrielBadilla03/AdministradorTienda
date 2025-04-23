using AdministradorTienda.EmailSender;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;

namespace AdministradorTienda.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ICustomEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, ICustomEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // No revelamos si el usuario no existe o no está confirmado.
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code)); // Codificación Base64 para el código.
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                try
                {
                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "Restablecer contraseña - FarmaciaSaule",
                        $"Hola, <br/><br/>Para restablecer tu contraseña, por favor haz clic en el siguiente enlace: <br/><a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Restablecer contraseña</a>.<br/><br/>Si no solicitaste este cambio, por favor ignora este correo.");
                }
                catch (Exception ex)
                {
                    // Manejar el error en el envío de correos si es necesario
                    ModelState.AddModelError(string.Empty, "Hubo un problema al enviar el correo. Intenta nuevamente.");
                    return Page();
                }

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
