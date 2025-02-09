﻿using NiORM.Test.Models;
using NiORM.Test.Service;
Console.WriteLine("First implement DataService with your models and set connection string");
Console.WriteLine("\n\n");
DataService dataService = new DataService("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=StoreDb;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

//for fetch data from DB
var people = dataService.People.ToList();

//for add a new person
var person = new Person() { Id=1, Age = 29,  Name = "Nima" };
dataService.People.Add(person);

person = dataService.People.FirstOrDefault();

//for edit the person
person.Age = 30;
dataService.People.Update(person);

//for Remove the person
dataService.People.Remove(person);