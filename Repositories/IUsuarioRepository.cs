using ChefMetrics_BackEnd.Model;

namespace ChefMetrics_BackEnd.Repositories
{
    public interface IUsuarioRepository
    {
        Task<bool> ExisteCorreoAsync(string correo);
        Task<UsuarioEmprendedor> RegistrarUsuarioAsync(UsuarioEmprendedor usuario);
    }
}