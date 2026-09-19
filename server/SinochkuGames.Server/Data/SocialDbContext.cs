using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SinochkuGames.Server.Data;

public sealed class SocialDbContext(DbContextOptions<SocialDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<InviteCode> InviteCodes => Set<InviteCode>();
    public DbSet<ProfileShowcase> ProfileShowcases => Set<ProfileShowcase>();
    public DbSet<ProfileComment> ProfileComments => Set<ProfileComment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Friendship>()
            .HasIndex(x => new { x.UserAId, x.UserBId })
            .IsUnique();

        builder.Entity<FriendRequest>()
            .HasIndex(x => new { x.SenderId, x.RecipientId, x.Status });

        builder.Entity<DirectMessage>()
            .HasIndex(x => new { x.SenderId, x.RecipientId, x.SentAtUtc });

        builder.Entity<NotificationEntity>()
            .HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        builder.Entity<GameSession>()
            .HasIndex(x => new { x.UserId, x.StartedAtUtc });

        builder.Entity<InviteCode>()
            .HasKey(x => x.Code);

        builder.Entity<ProfileShowcase>()
            .HasIndex(x => new { x.UserId, x.SortOrder });

        builder.Entity<ProfileComment>()
            .HasIndex(x => new { x.ProfileUserId, x.CreatedAtUtc });
    }
}
