using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TimeCapsule.API.Data.Repositories;
using TimeCapsule.API.Entities;
using TimeCapsule.API.Exceptions;
using TimeCapsule.API.Services;
using System.Net;
using System.Text;

namespace TimeCapsule.Tests.Services
{
    public class SpotifyServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ILogger<SpotifyService>> _loggerMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public SpotifyServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<SpotifyService>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationMock = new Mock<IConfiguration>();
        }

        [Fact]
        public async Task GetCurrentlyPlayingAsync_WhenUserDoesNotExist_ShouldThrowUserNotFoundException()
        {
            //Arrange
            int fakeUserId = 90;

            _userRepoMock
            .Setup(repo=>repo.FindUserWithSpotifyTokenAsync(fakeUserId))
            .ReturnsAsync((User?)null);

            var service = new SpotifyService(
                _userRepoMock.Object,
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );

            //act
            Func<Task> act = async () => await service.GetCurrentlyPlayingAsync(fakeUserId);

            //assert
            await Assert.ThrowsAsync<UserNotFoundException>(act);
        }
    
        [Fact]
        public async Task GetCurrentlyPlayingAsync_WhenEverythingGoesRight_ShouldReturnTrueAndTrackInfo()
        {
            //arrange
            int fakeUserId = 10;
            var fakeUser = new User
            {
                Id = fakeUserId,
                Username = "blabla",
                Email = "blabla@example.com",
                PasswordHash = "blablabla",
                SpotifyToken = new UserSpotifyToken
                {
                    Id = 1,
                    AccessToken = "aa_bb",
                    RefreshToken = "cc_dd",
                    ExpirationDate = DateTime.UtcNow.AddDays(1)
                }
            };

            _userRepoMock
            .Setup(repo=>repo.FindUserWithSpotifyTokenAsync(fakeUserId))
            .ReturnsAsync(fakeUser);

            var fakeJsonResponse = """
            {
                "item":
                    {
                        "artists":[
                        {
                            
                            "name":"artist_name"
                            
                        }
                        ],
                        "id":"track_id",
                        "name":"track_name"
                        
                    }
            }
            """;

            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeJsonResponse, Encoding.UTF8, "application/json")
            }
            );

            var fakeHttpClient = new HttpClient(handlerMock.Object);

            _httpClientFactoryMock
            .Setup(factory=>factory.CreateClient(It.IsAny<string>()))
            .Returns(fakeHttpClient);

            var service = new SpotifyService(
                _userRepoMock.Object,
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );

            //act
            var response = await service.GetCurrentlyPlayingAsync(fakeUserId);

            //assert
            Assert.Multiple(
                () => Assert.True(response.isSuccess),
                () => Assert.Null(response.ErrorMessage),
                () => Assert.Equal("track_id",response.TrackId),
                () => Assert.Equal("track_name",response.TrackName),
                () => Assert.Equal("artist_name",response.ArtistName) 
            );
        }   
    
        [Fact]
        public async Task GetCurrentlyPlayingAsync_WhenUserIsNotListening_ShouldReturnFalseWithCustomMessage()
        {
            //arrange
            int fakeUserId = 1;

            var fakeUser = new User
            {
                Id = fakeUserId,
                Username = "fake_user",
                PasswordHash = "blabla",
                Email = "aaa@example.com",
                SpotifyToken = new UserSpotifyToken
                {
                    Id = 1,
                    UserId = fakeUserId,
                    AccessToken = "access_token",
                    RefreshToken = "refresh_token",
                    ExpirationDate = DateTime.UtcNow.AddDays(1)
                }
            };

            _userRepoMock
            .Setup(repo=>repo.FindUserWithSpotifyTokenAsync(fakeUserId))
            .ReturnsAsync(fakeUser);

            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

            var fakeHttpClient = new HttpClient(handlerMock.Object);

            _httpClientFactoryMock
            .Setup(factory=>factory.CreateClient(It.IsAny<string>()))
            .Returns(fakeHttpClient);

            var service = new SpotifyService(
                _userRepoMock.Object,
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );            

            //act
            var response = await service.GetCurrentlyPlayingAsync(fakeUserId);

            //assert
            Assert.Multiple(
                () => Assert.False(response.isSuccess),
                () => Assert.Equal("No song is playing right now; please play a song and try again",response.ErrorMessage),
                () => Assert.Null(response.TrackId),
                () => Assert.Null(response.TrackName),
                () => Assert.Null(response.ArtistName)
            );
        }
    
        [Fact]
        public async Task GetCurrentlyPlayingAsync_WhenSpotifyApiFails_ShouldHandleGracefully()
        {
            //arrange
            int fakeUserId = 1;

            var fakeUser = new User
            {
                Id = fakeUserId,
                Username = "username",
                Email = "aaa@example.com",
                PasswordHash = "blabla",
                SpotifyToken = new UserSpotifyToken
                {
                    Id = 1,
                    UserId = fakeUserId,
                    AccessToken = "access_token",
                    RefreshToken = "refresh_token",
                    ExpirationDate = DateTime.UtcNow.AddDays(1)
                }                
            };

            _userRepoMock
            .Setup(repo=>repo.FindUserWithSpotifyTokenAsync(fakeUserId))
            .ReturnsAsync(fakeUser);

            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

            var fakeHttpClient = new HttpClient(handlerMock.Object);

            _httpClientFactoryMock
            .Setup(factory=>factory.CreateClient(It.IsAny<string>()))
            .Returns(fakeHttpClient);

            var service = new SpotifyService(
                _userRepoMock.Object,
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );

            //act
            Func<Task> act = async () => await service.GetCurrentlyPlayingAsync(fakeUserId);

            //assert
            var exception = await Assert.ThrowsAsync<Exception>(act);
        }
    }
}