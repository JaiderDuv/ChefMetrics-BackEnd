using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;

namespace ChefMetrics_BackEnd.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<bool> RegistrarEmprendedorFormalizadoAsync(EmprendedorFormalizado dto)
        {
            if (await _usuarioRepository.ExisteCorreoAsync(dto.CorreoElectronico))
            {
                throw new Exception("El correo electrónico ya se encuentra registrado.");
            }

            string hashPassword = PasswordHasher.HashPassword(dto.HashContrasena);

            var nuevoUsuario = new UsuarioEmprendedor
            {
                Formalizado = dto.Formalizado,
                Nit = dto.Nit,
                RazonSocial = dto.RazonSocial,
                CorreoElectronico = dto.CorreoElectronico,
                TipoDocumento = dto.TipoDocumento,
                DocumentoIdentidadUe = dto.DocumentoIdentidadUe,
                NombreEmprendimiento = dto.NombreEmprendimiento,
                CategoriaNegocio = dto.CategoriaNegocio,
                TelefonoContacto = dto.TelefonoContacto,
                CorreoElectronicoContacto = dto.CorreoElectronicoContacto,
                HashContrasena = hashPassword
            };

            // d. Persistir en base de datos
            var resultado = await _usuarioRepository.RegistrarUsuarioAsync(nuevoUsuario);
            return resultado != null;
        }

        public async Task<bool> RegistrarEmprendedorAsync(Emprendedor dto)
        {
            if (await _usuarioRepository.ExisteCorreoAsync(dto.CorreoElectronico))
            {
                throw new Exception("El correo electrónico ya se encuentra registrado.");
            }

            string hashPassword = PasswordHasher.HashPassword(dto.HashContrasena);

            var nuevoUsuario = new UsuarioEmprendedor
            {
                Formalizado = dto.Formalizado,
                Nit = null,
                RazonSocial = null,
                CorreoElectronico = dto.CorreoElectronico,
                TipoDocumento = dto.TipoDocumento,
                DocumentoIdentidadUe = dto.DocumentoIdentidadUe,
                NombreEmprendimiento = dto.NombreEmprendimiento,
                CategoriaNegocio = dto.CategoriaNegocio,
                TelefonoContacto = dto.TelefonoContacto,
                CorreoElectronicoContacto = dto.CorreoElectronicoContacto,
                HashContrasena = hashPassword
            };

            var resultado = await _usuarioRepository.RegistrarUsuarioAsync(nuevoUsuario);
            return resultado != null;
        }
    }
}