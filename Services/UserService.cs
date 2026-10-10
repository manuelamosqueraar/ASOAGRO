namespace Asoagro.Services
{
    public class UserService
    {
        public string? RolActual { get; private set; }
        public string? NombreUsuario { get; private set; }
        public bool EstaAutenticado => !string.IsNullOrEmpty(RolActual);

        public event Action? OnChange;

        public void IniciarSesion(string nombre, string rol)
        {
            NombreUsuario = nombre;
            RolActual = rol;
            NotifyStateChanged();
        }

        public void CerrarSesion()
        {
            NombreUsuario = null;
            RolActual = null;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}