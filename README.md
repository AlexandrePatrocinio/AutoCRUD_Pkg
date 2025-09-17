## AutoCRUD

*Português (pt-br)

Uma extensão simples para criar automaticamente um CRUD para projetos .NET usando minimal APIs no .NET 8.

Ele é usado para criar rapidamente endpoints para operações de criação, leitura, atualização e exclusão de uma entidade específica por meio dos métodos de extensão das classes base WebApplicationBuilder e WebApplication. Estas extensões estão disponiveis no namespace AutoCRUD.Extensions. 

Também permite integrar validações personalizadas para cada endpoint usando a interface IServiceAutoCRUDValidation disponível no namespace AutoCRUD.Services.

Inicialmente ela possui integração com o postgresSQL et SQLServer através de duas implementações genéricas da interface IRepository disponíveis nos namespaces AutoCRUD.Data.NpgSql et AutoCRUD.Data.SqlClient respectivamente. Para isso, AutoCRUD utilisa o pacote Dapper.

Para saber como usá-lo favor verificar o repositório de exemplo chamado "api" (https://github.com/AlexandrePatrocinio/api) em meu github.

### Projeto
Esta extensão foi inspirada no desafio que ocorreu no Brasil em 2023 chamado rinhabackend (https://github.com/zanfranceschi/rinha-de-backend-2023-q3)
Usei a ideia para gerar de maneira facil uma api que implementa um CRUD automaticamente (dai o  nome) com os endpoints solicitados no desafio.

*Français (fr-fr)

Une extension simple pour créer automatiquement un CRUD pour les projets .NET en utilisant un minimal API dans .NET 8.

Il est utilisé pour créer rapidement des points de terminaison pour les opérations de création, de lecture, de mise à jour et de suppression pour une entité spécifique via les méthodes d'extension des classes de base WebApplicationBuilder et WebApplication. Ces extensions sont disponibles dans l'espace de noms AutoCRUD.Extensions.

Il vous permet également d'intégrer des validations personnalisées pour chaque point de terminaison à l'aide de l'interface IServiceAutoCRUDValidation disponible dans l'espace de noms AutoCRUD.Services.

Initialement, il est intégré à PostgresSQL et SQLServer via deux implémentations génériques de l'interface IRepository disponibles dans les espaces de noms AutoCRUD.Data.NpgSql et AutoCRUD.Data.SqlClient respectivement. Pour cela, AutoCRUD utilise le package Dapper.

Pour savoir comment l'utiliser, veuillez consulter l'exemple de référentiel appelé "api" (https://github.com/AlexandrePatrocinio/api) sur mon github.

### Projet
Cet extension a été inspirée par le défi qui a eu lieu au Brésil en 2023 appelé rinhabackend (https://github.com/zanfranceschi/rinha-de-backend-2023-q3).
J’ai utilisé l’idée pour générer facilement une API qui met en place un CRUD automatiquement (d’où le nom) avec les points de terminaison demandés dans le défi.

*English (en-us)

A simple extension to automatically create a CRUD for .NET projects using a minimal API in .NET 8.

It is used to quickly create endpoints for create, read, update, and delete operations for a specific entity via the extension methods of the WebApplicationBuilder and WebApplication base classes. These extensions are available in the AutoCRUD.Extensions namespace.

It also allows you to integrate custom validations for each endpoint using the IServiceAutoCRUDValidation interface available in the AutoCRUD.Services namespace.

Initially, it is integrated with PostgresSQL and SQLServer via two generic implementations of the IRepository interface available in the AutoCRUD.Data.NpgSql and AutoCRUD.Data.SqlClient namespaces respectively. For this, AutoCRUD uses the Dapper package.

To learn how to use it, please see the example repository called "api" (https://github.com/AlexandrePatrocinio/api) on my github.

### Project
This extension was inspired by the challenge that occurred in Brazil in 2023 called rinhabackend (https://github.com/zanfranceschi/rinha-de-backend-2023-q3).
I used the idea to easily generate an API that implements a CRUD automatically (hence the name) with the endpoints requested in the challenge.

### Endpoints examples

**POST http://domain.com/persons
body
{
    "Alias": "Ben",
    "Name": "Bernardo Barbosa Silva",
    "Birthdate": "1991-10-09",
    "Stack": ["C#","Unity","Postgres"]
}**

*RESPONSE http 201 Created
body
{
    "id": "664ffa0e-2d4d-4516-858c-39aab60b1aa4",
    "alias": "Ben",
    "name": "Bernardo Barbosa Silva",
    "birthdate": "1991-10-09T00:00:00",
    "stack": [
        "C#",
        "Unity",
        "Postgres"
    ]
}*
--------------------------------------------------------------------------

**POST http://domain.com/persons/batch
body
[
    {
        "alias": "Ale",
        "name": "Alexandre Roberto Alcantra",
        "birthdate": "1996-02-21T00:00:00",
        "stack": ["kotlin","mobile","MongoDb"]
    },
    {
        "alias": "ALP",
        "name": "Alexandre Lino Pereira",
        "birthdate": "1998-05-17T00:00:00",
        "stack": ["elixir","Fenix","MariaDB"]
    },
    {
        "alias": "Bar",
        "name": "Bruno Almeida Leite",
        "birthdate": "1990-01-13T00:00:00",
        "stack": ["F#","MAUI","CosmoDB"]
    }    
]**

*RESPONSE http 201 Created
body
3 /*Number of entities created*/*
--------------------------------------------------------------------------

**PUT http://domain.com/persons
body
{
    "id": "664ffa0e-2d4d-4516-858c-39aab60b1aa4",
    "alias": "Ben",
    "name": "Bernardo Barbosa Silva",
    "birthdate": "1990-10-09T00:00:00",
    "stack": ["C#","Unity","MongoDB"]
}**

*RESPONSE http 200 OK
body
{
    "id": "664ffa0e-2d4d-4516-858c-39aab60b1aa4",
    "alias": "Ben",
    "name": "Bernardo Barbosa Silva",
    "birthdate": "1990-10-09T00:00:00",
    "stack": [
        "C#",
        "Unity",
        "MongoDB"
    ]
}*
--------------------------------------------------------------------------

**GET http://domain.com/persons?t=ale /*t = search term*/**

*RESPONSE http 200 OK
body
[
    {
        "id": "da1f440c-b603-462b-8fc2-d5e4425719a9",
        "alias": "Ale",
        "name": "Alexandre Roberto Alcantra",
        "birthdate": "1996-02-21T00:00:00",
        "stack": [
            "kotlin",
            "mobile",
            "MongoDb"
        ]
    },
    {
        "id": "ec93f941-32d2-45d2-928f-96ffa81b1759",
        "alias": "ALP",
        "name": "Alexandre Lino Pereira",
        "birthdate": "1998-05-17T00:00:00",
        "stack": [
            "elixir",
            "Fenix",
            "MariaDB"
        ]
    }
]*
--------------------------------------------------------------------------

**GET http://domain.com/persons?o=Alias&pg=2&sz=3 /*o = order by field; pg = page number; sz = page size*/**

*RESPONSE http 200 OK
body
[
    {
        "id": "d4f7779c-8887-4b69-894a-15de363074b2",
        "alias": "Bar",
        "name": "Bruno Almeida Leite",
        "birthdate": "1990-01-13T00:00:00",
        "stack": [
            "F#",
            "MAUI",
            "CosmoDB"
        ]
    },
    {
        "id": "604a4956-9518-41a5-8bc4-30116751e7f5",
        "alias": "Lau",
        "name": "Lauro Mendonça Magalhães",
        "birthdate": "1985-07-06T00:00:00",
        "stack": [
            "Java",
            "React",
            "Oracle"
        ]
    },
    {
        "id": "664ffa0e-2d4d-4516-858c-39aab60b1aa4",
        "alias": "Ben",
        "name": "Bernardo Barbosa Silva",
        "birthdate": "1990-10-09T00:00:00",
        "stack": [
            "C#",
            "Unity",
            "MongoDB"
        ]
    }
]*
--------------------------------------------------------------------------

**GET http://domain.com/persons/e68df3dd-ee28-4067-a4ea-dca421828306**

*RESPONSE http 200 OK
body
{
    "id": "e68df3dd-ee28-4067-a4ea-dca421828306",
    "alias": "Mia",
    "name": "Michelle Miranda Golveia",
    "birthdate": "1996-09-05T00:00:00",
    "stack": [
        "Javascript",
        "node",
        "MariaDB"
    ]
}*
--------------------------------------------------------------------------

**DELETE http://domain.com/persons/e55fa387-2db1-4c01-99ac-89906c2b8cae**

*RESPONSE http 200 OK
body
{
    "id": "e55fa387-2db1-4c01-99ac-89906c2b8cae",
    "alias": "LMM",
    "name": "Lauro Mendonça Magalhães",
    "birthdate": "1985-07-06T00:00:00",
    "stack": [
        "Java",
        "React",
        "Oracle"
    ]
}*
--------------------------------------------------------------------------

**GET http://domain.com/count-persons**

*RESPONSE http 200 OK
body
32*

### Package dependencies

- Dapper Version="2.1.66"
- Microsoft.AspNetCore.Mvc Version="2.3.0"
- Npgsql Version="9.0.3"
- Microsoft.Data.SqlClient Version="6.1.0"
- System.Data.SqlClient Version="4.9.0
