namespace _20263RecetasApi.Auth
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Recepcion = "Recepcion";

        // Para [Authorize(Roles = Roles.Personal)]: basta con tener cualquiera de los dos
        public const string Personal = Admin + "," + Recepcion;

        // Roles que se crean al arrancar (seeder) y que se pueden asignar a un usuario
        public static readonly string[] Todos = { Admin, Recepcion };
    }
}
