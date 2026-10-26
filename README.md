AutoMapper; usar los datos de mi modelos a mis DTOs y viceversa
ASPNETCORE.identity. La autenticacion de microsoft y nos genera tablas en nuestra base de datos para usuarios, roles y otros.
Aspnetcore.newconsoftjson
Aspnetcore.OpenApi genera la documentacion y para probar los endpoint
entityFrameworkcore. Para manejar de mejor la base de datos con el codigo .net 
entityFrameworkcore.tools Para manejar la base de datos y traducir el lenguaje .net a el lenguaje de la base de datos y crear la migracion
npgsql.EntityFramework.postgreSQL Para podernos conectarnos a la base de datos
Scalar.Aspnetcore Es igual que en openApi, sirve para hacer nuestra documentacion y probar nuestros endpoints
Investiar que es middleware
1.-Instalar dependencias que vayamos a utilizar
2.-Configurar nuestro archivo program.cs(el cerebro de nuestro programa)
3.-Configurar archivo Appsetting.json(La cadena de conexion y la llaveJWT)
AllowedHosts(Dispositivos que pueden hacer consultas a la Api mediante ip, "Investigar")
4.-crear carpeta applicationUser  (nombre de usuario)
5.-Crear el contexto de datos(ApplicationDbcontext) 
6.-Crear los modelos y agregarlos al contextp de datos(ApplicationDbContext)
Concepto de ORM
7.-Hacer la migracion(QUE NO SEA IGUAL EL NOMBRE A UNA CLASE QUE YA ESTA HECHA)
Ejemplos; AddRecetas, CRecetas(modificar un cambo). Despues usar el update database. 
Si quiero agregar una columna nueva a la tabla y la migracion esta creada. Debes eliminar la migracion y volver a crearla.

8.-Crear los DTOS