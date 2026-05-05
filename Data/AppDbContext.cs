using Microsoft.EntityFrameworkCore;
using TaskBoard.Models;

namespace TaskBoard.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Workspace> Workspaces { get; set; }

    public DbSet<Board> Boards { get; set; }

    public DbSet<BoardList> Lists { get; set; }

    public DbSet<Card> Cards { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }

    public DbSet<Label> Labels { get; set; }

    public DbSet<CardLabel> CardLabels { get; set; }

    public DbSet<CardMember> CardMembers { get; set; }

    public DbSet<Comment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Board>()
            .HasOne(b => b.Workspace)
            .WithMany()
            .HasForeignKey(b => b.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BoardList>()
            .HasOne(l => l.Board)
            .WithMany()
            .HasForeignKey(l => l.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Card>()
            .HasOne(c => c.List)
            .WithMany()
            .HasForeignKey(c => c.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.TokenHash).IsUnique();

            entity.Ignore(r => r.IsActive);
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.HasKey(wm => new { wm.WorkspaceId, wm.UserId });

            entity.HasOne(wm => wm.Workspace)
                .WithMany()
                .HasForeignKey(wm => wm.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(wm => wm.User)
                .WithMany()
                .HasForeignKey(wm => wm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasOne(l => l.Workspace)
                .WithMany()
                .HasForeignKey(l => l.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardLabel>(entity =>
        {
            entity.HasKey(cl => new { cl.CardId, cl.LabelId });

            entity.HasOne(cl => cl.Card)
                .WithMany(c => c.Labels)
                .HasForeignKey(cl => cl.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cl => cl.Label)
                .WithMany()
                .HasForeignKey(cl => cl.LabelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardMember>(entity =>
        {
            entity.HasKey(cm => new { cm.CardId, cm.UserId });

            entity.HasOne(cm => cm.Card)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cm => cm.User)
                .WithMany()
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasOne(c => c.Card)
                .WithMany(card => card.Comments)
                .HasForeignKey(c => c.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

