﻿using System.Collections.Generic;
using System.Linq;
using Norm.BSON.DbTypes;
using Xunit;

namespace Norm.Tests
{
    public class DbRefTests
    {
        [Fact]
        public void DbRefMapsToOtherDocumentsByOid()
        {
            const string databaseName = "NormTests";
            var id = ObjectId.NewObjectId();

            using (var session = new Session())
            {
                session.Drop<Product>();
                session.Drop<ProductReference>();

                session.Add(new Product { _id = id, Name = "RefProduct" });

                var productReference = new DbReference<Product>(id);

                session.Add(new ProductReference
                    {
                        Id = ObjectId.NewObjectId(),
                        Name = "FullCart",
                        ProductsOrdered = new[] { productReference }
                    });
            }

            var server = Mongo.Create("mongodb://localhost/" + databaseName);
            var reference = server.GetCollection<ProductReference>().Find().First();
            var product = reference.ProductsOrdered[0].Fetch(() => server);

            Assert.Equal(id.Value, product._id.Value);
        }

        [Fact]
        public void DbRefMapsToOtherDocumentsByCustomId()
        {
            const string databaseName = "NormTests";
            const string userId = "Tim Berners-Lee";
            const string roleName = "Administrator";

            using (var session = new Session())
            {
                session.Drop<User>();
                session.Drop<Role>();

                session.Add(new User
                                {
                                    Id = userId,
                                    EmailAddress = "user@domain.com"
                                });
                session.Add(new Role
                                {
                                    Id = roleName,
                                    Users = new List<DbReference<User, string>>
                                                {
                                                    new DbReference<User, string>(userId)
                                                }
                                });
            }

            var server = Mongo.Create("mongodb://localhost/" + databaseName);
            var role = server.GetCollection<Role>().Find().First();
            var user = role.Users[0].Fetch(() => server);

            Assert.Equal(userId, user.Id);
        }
    }
}