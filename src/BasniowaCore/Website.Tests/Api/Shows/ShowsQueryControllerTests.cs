﻿using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using DataAccess.Shows;
using FluentAssertions;
using Logic.Common;
using Logic.Shows;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Website.Api.Shows;
using Website.Infrastructure.ErrorHandling;
using Xunit;

namespace Website.Tests.Api.Shows
{
    public class ShowsQueryControllerTests
    {
        public IShowsReader ShowsReader { get; set; }

        public IMapper Mapper { get; set; }

        public ShowsQueryControllerTests()
        {
            var config = new MapperConfiguration(ShowsQueryController.ConfigureMapper);
            Mapper = new Mapper(config);

            ShowsReader = Mock.Of<IShowsReader>();
        }

        public ShowsQueryController Create()
        {
            return new ShowsQueryController
            {
                ShowsReader = ShowsReader,
                Mapper = Mapper
            };
        }

        [Fact(DisplayName = nameof(ShowsCommandControllerTests) + ": GetAllShows should return correct list of shows.")]
        public void GetAllShowsSuccessful()
        {
            // arrange
            var shows = new List<ShowHeader>
            {
                new ShowHeader { ShowId = 1, Title = "T1", Subtitle = "S1" },
                new ShowHeader { ShowId = 2, Title = "T2", Subtitle = "S2" },
            };
            Mock.Get(ShowsReader).Setup(x => x.GetAllShows()).Returns(shows);
            var controller = Create();

            // act
            var actual = controller.GetAll();

            actual.Select(x => new { Id = x.ShowId, x.Title, x.Subtitle })
                .Should().BeEquivalentTo(
                shows.Select(x => new { Id = x.ShowId, x.Title, x.Subtitle }));
        }

        [Fact(DisplayName = nameof(ShowsCommandControllerTests) + ": GetById should return an existing show.")]
        public void GetByIdExisting()
        {
            // arrange
            long id = 123;
            var show = new ShowWithDetails
            {
                ShowId = id,
                Title = "T",
                Subtitle = "S",
                Description = "D",
                Properties = new Dictionary<string, string>
                {
                    ["P1"] = "V1",
                    ["P2"] = "V2",
                }
            };
            Mock.Get(ShowsReader).Setup(x => x.GetShowById(id)).Returns(show);
            var controller = Create();

            // act
            var actual = controller.GetById(id);

            actual.ShowId.Should().Be(show.ShowId);
            actual.Title.Should().Be(show.Title);
            actual.Subtitle.Should().Be(show.Subtitle);
            actual.Description.Should().Be(show.Description);
            actual.Properties.ShouldBeEquivalentTo(show.Properties);
        }

        [Fact(DisplayName = nameof(ShowsCommandControllerTests) + ": GetById should throw when the show doesn't exist.")]
        public void GetByIdNonExisting()
        {
            // arrange
            long id = 123;
            Mock.Get(ShowsReader).Setup(x => x.GetShowById(id)).Throws(new EntityNotFoundException<Show>(id.ToString()));
            var controller = Create();

            // act
            var actualException = Assert.Throws<HttpErrorException>(() => controller.GetById(id));

            actualException.ActionResult.Should().BeOfType<NotFoundResult>();
        }
    }
}