using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Services
{
    public interface IUsuarioService
    {
        Task<bool> RegistrarEmprendedorFormalizadoAsync(EmprendedorFormalizado dto);
        Task<bool> RegistrarEmprendedorAsync(Emprendedor dto);
        Task<bool> ValidarTokenAsync(string token);

        Task<bool> ReenviarCorreoVerificacionAsync(string correo);
    }
}
