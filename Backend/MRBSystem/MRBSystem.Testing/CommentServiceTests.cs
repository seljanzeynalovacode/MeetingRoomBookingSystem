using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;

namespace MRBSystem.Testing;

public class CommentServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly Guid _authorId = Guid.NewGuid();

    public CommentServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        _context.Users.Add(new AppUser
        {
            Id = _authorId,
            FirstName = "Author",
            LastName = "User",
            UserName = "author",
            Email = "author@example.com"
        });
        _context.SaveChanges();
    }

    private CommentService CreateService(Guid currentUserId)
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(currentUserId);

        return new CommentService(
            new GenericRepository<Comment>(_context),
            new GenericRepository<Like>(_context),
            currentUser.Object,
            mapperConfig.CreateMapper(),
            new UnitOfWork(_context));
    }

    [Fact]
    public async Task DeleteAsync_RootWithNestedReplies_DeletesEntireThread()
    {
        var root = new Comment { Content = "Root", UserId = _authorId };
        _context.Comments.Add(root);
        await _context.SaveChangesAsync();

        var reply = new Comment { Content = "Reply", UserId = _authorId, ParentCommentId = root.Id };
        _context.Comments.Add(reply);
        await _context.SaveChangesAsync();

        var nestedReply = new Comment { Content = "Nested reply", UserId = _authorId, ParentCommentId = reply.Id };
        _context.Comments.Add(nestedReply);
        await _context.SaveChangesAsync();

        var service = CreateService(_authorId);

        await service.DeleteAsync(root.Id);

        var remainingComments = _context.Comments.ToList();
        remainingComments.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ByNonOwner_ThrowsUnauthorized()
    {
        var comment = new Comment { Content = "Root", UserId = _authorId };
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        var service = CreateService(Guid.NewGuid());

        var act = () => service.DeleteAsync(comment.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        var remainingComments = _context.Comments.ToList();
        remainingComments.Should().ContainSingle(c => c.Id == comment.Id);
    }

    [Fact]
    public async Task LikeAsync_CalledTwiceBySameUser_OnlyCreatesOneLike()
    {
        var comment = new Comment { Content = "Root", UserId = _authorId };
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        var service = CreateService(_authorId);

        await service.LikeAsync(comment.Id);
        await service.LikeAsync(comment.Id);

        var likeCount = _context.Likes.Count(l => l.CommentId == comment.Id);
        likeCount.Should().Be(1);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
