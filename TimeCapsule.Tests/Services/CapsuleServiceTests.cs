using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TimeCapsule.API.Data.Repositories;
using TimeCapsule.API.Services;
using Xunit;
using TimeCapsule.API.Entities;
using Xunit.Sdk;
using System.Globalization;
using Moq.Protected;
using System.Net;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace TimeCapsule.Tests.Services
{
    public class CapsuleServiceTests
    {
        private readonly Mock<ICapsuleRepository> _capsuleRepoMock;
        private readonly Mock<ILogger<CapsuleService>> _loggerMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

        public CapsuleServiceTests()
        {
            _capsuleRepoMock = new Mock<ICapsuleRepository>();
            _loggerMock = new Mock<ILogger<CapsuleService>>();            
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        }

        [Fact]
        public async Task TryUnlockSpotifyCapsuleAsync_WhenTrackMatches_ShouldUnlockCapsuleAndReturnSuccess()
        {
            //arrange
            int targetId = 1;
            int userId = 1;
            string trackId = "aaa333";

            var fakeCapsulesList = new List<CapsuleEntity>
            {
                new CapsuleEntity
                {    
                Id = targetId,
                EncryptedContent = "A Secret Message",
                Category = TriggerType.SpotifyTrackId,
                TargetValue = trackId,
                UserID = userId
                }
            };

            _capsuleRepoMock
            .Setup(repo=> repo.GetPendingSpotifyCapsulesAsync(userId,trackId))
            .ReturnsAsync(fakeCapsulesList);

            _capsuleRepoMock
            .Setup(repo=> repo.SaveAsync())
            .ReturnsAsync(true);

            var service = new CapsuleService(
                _capsuleRepoMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object
            );

            //act
            var result = await service.TryUnlockSpotifyCapsuleAsync(userId, trackId);

            //assert
            Assert.True(result.isSuccess);
            Assert.NotNull(result.openedCapsuleIds);
            Assert.Contains(targetId,result.openedCapsuleIds);

            _capsuleRepoMock.Verify(repo=>repo.GetPendingSpotifyCapsulesAsync(userId,trackId), Times.Once);
            _capsuleRepoMock.Verify(repo=>repo.SaveAsync() ,Times.Once);
        }        

        [Fact]
        public async Task TryUnlockSpotifyCapsuleAsync_WhenNoMatchingCapsules_ShouldReturnFailureAndNotSaveChanges()
        {
            //arrange
            int userId = 1;
            string unmatchedTrackId = "unmatched_track_id";

            _capsuleRepoMock
            .Setup(repo=> repo.GetPendingSpotifyCapsulesAsync(userId,unmatchedTrackId))
            .ReturnsAsync(new List<CapsuleEntity>());

            var service = new CapsuleService(
                _capsuleRepoMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object
            );

            //act

            var result = await service.TryUnlockSpotifyCapsuleAsync(userId,unmatchedTrackId);

            //assert

            Assert.False(result.isSuccess);
            Assert.Null(result.openedCapsuleIds);

            _capsuleRepoMock.Verify(repo=>repo.SaveAsync(), Times.Never);        
        }

        [Fact]
        public async Task ProcessBackgroundTriggersAsync_WhenDateCapsuleIsDue_ShouldUnlockAndSave()
        {
            DateTime targetDate = DateTime.UtcNow.AddDays(-1);

            CultureInfo culture = CultureInfo.InvariantCulture;

            string formattedDate = targetDate.ToString(culture);
            //arrange
            var dueCapsule = new CapsuleEntity
            {
                Id = 10,
                UserID = 1,
                Category = TriggerType.Date,
                EncryptedContent = "A Message about date",
                TargetValue = formattedDate
            };

            var pendingList = new List<CapsuleEntity> { dueCapsule };

            _capsuleRepoMock
            .Setup(repo=> repo.GetPendingCapsulesForBackroundAsync(It.IsAny<IEnumerable<TriggerType>>()))
            .ReturnsAsync(pendingList);

            _capsuleRepoMock
            .Setup(repo => repo.SaveAsync())
            .ReturnsAsync(true);

            var service = new CapsuleService(
                _capsuleRepoMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object
            );

            //act

            await service.ProcessBackgroundTriggersAsync();

            //assert
            Assert.True(dueCapsule.IsOpened);

            _capsuleRepoMock.Verify(repo=>repo.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessBackgroundTriggersAsync_WhenDateCapsuleIsNotDue_ShouldNotUnlockAndNotSave()
        {
            //arrange
            DateTime dateTime = DateTime.UtcNow.AddDays(1);
            CultureInfo culture = CultureInfo.InvariantCulture;
            string formattedDate = dateTime.ToString(culture);
            var futureCapsule = new CapsuleEntity
            {
                Id = 11,
                UserID = 1,
                Category = TriggerType.Date,
                TargetValue = formattedDate,
                EncryptedContent = "A time based message"
            };
            var pendingList = new List<CapsuleEntity>{ futureCapsule };
            _capsuleRepoMock
            .Setup(repo=>repo.GetPendingCapsulesForBackroundAsync(It.IsAny<IEnumerable<TriggerType>>()))
            .ReturnsAsync(pendingList);
            var service = new CapsuleService(
                _capsuleRepoMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object
            );
            //act
            await service.ProcessBackgroundTriggersAsync();
            //assert
            Assert.False(futureCapsule.IsOpened);
            _capsuleRepoMock.Verify(repo=>repo.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task ProcessBackgroundTriggersAsync_WhenWeatherThresholdMet_ShouldUnlockAndSave()
        {
            //arrange
            var weatherCapsule = new CapsuleEntity
            {
                Id = 20,
                UserID = 1,
                Category = TriggerType.Weather,
                EncryptedContent = "A message about weather",
                TargetValue = "25",
                Operator = TriggerOperator.GreaterThan
            };

            var pendingList = new List<CapsuleEntity> { weatherCapsule };

            _capsuleRepoMock
            .Setup(repo=>repo.GetPendingCapsulesForBackroundAsync(It.IsAny<IEnumerable<TriggerType>>()))
            .ReturnsAsync(pendingList);

            _capsuleRepoMock
            .Setup(repo=>repo.SaveAsync())
            .ReturnsAsync(true);

            var fakeJsonResponse = "{\"current_weather\":{\"temperature\":31.3}}";

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
                Content = new StringContent(fakeJsonResponse)
            });

            var fakeHttpClient = new HttpClient(handlerMock.Object);

            _httpClientFactoryMock
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(fakeHttpClient);

            var service = new CapsuleService(
                _capsuleRepoMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object
            );

            //act
            await service.ProcessBackgroundTriggersAsync();

            //assert
            Assert.True(weatherCapsule.IsOpened);
            _capsuleRepoMock.Verify(repo=>repo.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessBackgroundTriggersAsync_WhenCryptoTargetPriceReached_ShouldUnlockAndSave()
        {
            //arrange
            var cryptoCapsule = new CapsuleEntity
            {
                Id = 30,
                UserID  = 1,
                Category = TriggerType.Crypto,
                TargetValue = "60000",
                MetaData = "{\"symbol\":\"BTCUSDT\"}",
                EncryptedContent = "A message about crypto",
                Operator = TriggerOperator.GreaterThan
            };
            var pendingList = new List<CapsuleEntity> { cryptoCapsule };

            _capsuleRepoMock
            .Setup(repo=>repo.GetPendingCapsulesForBackroundAsync(It.IsAny<IEnumerable<TriggerType>>()))
            .ReturnsAsync(pendingList);
            _capsuleRepoMock
            .Setup(repo=>repo.SaveAsync())
            .ReturnsAsync(true);

            var fakeCryptoResponse = "{\"symbol\":\"BTCUSDT\",\"price\":\"65000.00\"}";

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
                Content = new StringContent(fakeCryptoResponse) 
            });

            var fakeHttpClient = new HttpClient(handlerMock.Object);

            _httpClientFactoryMock
            .Setup(factory=>factory.CreateClient(It.IsAny<String>()))
            .Returns(fakeHttpClient);

            var service = new CapsuleService(
                _capsuleRepoMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object
            );
            //act
            await service.ProcessBackgroundTriggersAsync();
            //assert
            Assert.True(cryptoCapsule.IsOpened);
            _capsuleRepoMock.Verify(repo=>repo.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessBackgroundTriggersAsync_WhenExternalApiFails_ShouldHandleGracefullyAndNotSave()
        {
            //arrange
            var cryptoCapsule = new CapsuleEntity
            {
                Id = 90,
                EncryptedContent = "A message",
                UserID = 1,
                Category = TriggerType.Crypto,
                TargetValue = "60000",
                MetaData = "{\"symbol\":\"BTCUSDT\"}",
                Operator = TriggerOperator.GreaterThan
            };
            var pendingList = new List<CapsuleEntity> { cryptoCapsule };

            _capsuleRepoMock
            .Setup(repo=>repo.GetPendingCapsulesForBackroundAsync(It.IsAny<IEnumerable<TriggerType>>()))
            .ReturnsAsync(pendingList);

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

            var service = new CapsuleService(
                _capsuleRepoMock.Object,
                _httpClientFactoryMock.Object,
                _loggerMock.Object
            );

            //act
            await service.ProcessBackgroundTriggersAsync();

            //assert
            Assert.False(cryptoCapsule.IsOpened);
            _capsuleRepoMock.Verify(repo=>repo.SaveAsync(), Times.Never);
        }
    }
}