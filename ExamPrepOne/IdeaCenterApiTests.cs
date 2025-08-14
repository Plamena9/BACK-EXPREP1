using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using IdeaCenterExamPrepOne.Models;



namespace IdeaCenterExamPrepOne
{
    [TestFixture]
    public class IdeaCenterApiTests
    {
        private RestClient client;
        private static string lastCreatedIdeaID;
        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string StatickToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIzN2MzNjdlMC1hMGEzLTQ0NTctODg4MS00MjNjZTVmMTIxMjQiLCJpYXQiOiIwOC8xMy8yMDI1IDEwOjExOjQyIiwiVXNlcklkIjoiN2JhN2JhZTItZGYzOC00NzdmLWQyOWUtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJleGFtQHByZXAxLmNvbSIsIlVzZXJOYW1lIjoiZXhhbXByZXAxUGxhbXMiLCJleHAiOjE3NTUxMDE1MDIsImlzcyI6IklkZWFDZW50ZXJfQXBwX1NvZnRVbmkiLCJhdWQiOiJJZGVhQ2VudGVyX1dlYkFQSV9Tb2Z0VW5pIn0.6nQEc8Z8tkt23-zhqfxGLbsPagUHRebgp8hhi3M6dUQ"; // for renewal of token
        private static string LoginEmail = "exam@prep1.com";
        private static string LoginPassword = "123456789";


        [OneTimeSetUp]// used because we need to authenticate only once before all tests and get the JWT token
        public void Setup()
        {
            string jwtToken;
            if (!string.IsNullOrWhiteSpace(StatickToken))
            {
                jwtToken = StatickToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }
        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { email, password }); // we make an object with the email and password to send in the request body because AddJsonBody expects an ===object=== or a ===string=== AddJsonBody is defined to take one argument: an object to be serialized to JSON.
   // this is another way to send a JSON body in RestSharp. By creating a model class or object in the Models folder ant this:
                                                          //var loginRequest = new LoginRequest
                                                          //{
                                                          //    Email = email,
                                                          //    Password = password
                                                          //};
                                                          //request.AddJsonBody(loginRequest);


            var responce = tempClient.Execute(request);

            if (responce.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(responce.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JTW token from the responce.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {responce.StatusCode}, Conten:{responce.Content}");
            }
        }

        //all tests here

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequirededFields_ShouldReturnSuccess()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea 1",
                Description = "Idea description 1",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var responce = this.client.Execute(request);

            var createResponce = JsonSerializer.Deserialize<ApiResponseDTO>(responce.Content);

            Assert.That(responce.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(createResponce.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]

        public void GetAllIdeas_ShouldReturnListOfIdeas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var responce = this.client.Execute(request);

            var responceItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(responce.Content);

            Assert.That(responce.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responceItems, Is.Not.Null);
            Assert.That(responceItems, Is.Not.Empty);

            lastCreatedIdeaID = responceItems.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]

        public void EditTheLastCreatedIdea_ShouldReturnSuccess()
        { 
            var editIdeaRequest = new IdeaDTO
            {
                Title = "Updated Test Idea",
                Description = "Updated description",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaID);
            request.AddJsonBody(editIdeaRequest);

            var responce = this.client.Execute(request);
            var editResponce = JsonSerializer.Deserialize<ApiResponseDTO>(responce.Content);

            Assert.That(responce.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponce.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]

        public void DeleteTheLastEditedIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaID);

            var responce = this.client.Execute(request);
          //  var deleteResponce = JsonSerializer.Deserialize<ApiResponseDTO>(responce.Content);

            Assert.That(responce.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responce.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]
        public void TryCreatingNewIdea_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var badIdeaRequest = new IdeaDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(badIdeaRequest);
            var responce = this.client.Execute(request);

            var errorResponse = JsonSerializer.Deserialize<ApiResponseDTO>(responce.Content);
            Assert.That(responce.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]

        [Test]
        public void TryEditing_NonExistingIdea_ShouldReturnBadRequest()
        {
            var editIdeaRequest = new IdeaDTO
            {
                Title = "Non-existing Idea",
                Description = "This idea does not exist",
                Url = ""
            };
            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", "404"); // Non-existing ID
            request.AddJsonBody(editIdeaRequest);
            var responce = this.client.Execute(request);
          
            Assert.That(responce.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(responce.Content, Does.Contain("There is no such idea!"));
        }

        [Order(7)]
        [Test]
        public void TryDeleting_NonExistingIdea_ShouldReturnBadRequest()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", "404");

            var responce = this.client.Execute(request);
          
            Assert.That(responce.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(responce.Content, Does.Contain("There is no such idea!"));
        }


        [OneTimeTearDown]
        public void TearDown()
        {
            this.client.Dispose();
        }

    }
}