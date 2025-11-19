using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public class ForumCategory
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(200)]
        public string Slug { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }

    public enum PostStatus
    {
        Draft,
        Published,
        Archived,
        Deleted
    }

    public class Post
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? VideoUrl { get; set; }

        public string? Tags { get; set; } 

        public PostStatus Status { get; set; } = PostStatus.Published;

        public bool IsPinned { get; set; } = false;

        public bool IsLocked { get; set; } = false;

        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastActivityAt { get; set; }

        public Guid UserId { get; set; }
        public Guid CategoryId { get; set; }

        [JsonIgnore]
        public User User { get; set; } = null!;

        [JsonIgnore]
        public ForumCategory Category { get; set; } = null!;

        [JsonIgnore]
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [JsonIgnore]
        public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();

        [JsonIgnore]
        public ICollection<PostReport> Reports { get; set; } = new List<PostReport>();
    }

    public class Comment
    {
        public Guid Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsEdited { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public Guid? ParentCommentId { get; set; }

        [JsonIgnore]
        public Post Post { get; set; } = null!;

        [JsonIgnore]
        public User User { get; set; } = null!;

        [JsonIgnore]
        public Comment? ParentComment { get; set; }

        [JsonIgnore]
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        [JsonIgnore]
        public ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();
    }

    public enum ReactionType
    {
        Like,
        Love,
        Helpful,
        Insightful
    }

    public class PostReaction
    {
        public Guid Id { get; set; }

        public ReactionType Type { get; set; } = ReactionType.Like;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid PostId { get; set; }
        public Guid UserId { get; set; }

        [JsonIgnore]
        public Post Post { get; set; } = null!;

        [JsonIgnore]
        public User User { get; set; } = null!;
    }

    public class CommentReaction
    {
        public Guid Id { get; set; }

        public ReactionType Type { get; set; } = ReactionType.Like;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid CommentId { get; set; }
        public Guid UserId { get; set; }

        [JsonIgnore]
        public Comment Comment { get; set; } = null!;

        [JsonIgnore]
        public User User { get; set; } = null!;
    }

    public enum ReportReason
    {
        Spam,
        Harassment,
        OffTopic,
        Inappropriate,
        Misinformation,
        Other
    }

    public enum ReportStatus
    {
        Pending,
        UnderReview,
        Resolved,
        Dismissed
    }

    public class PostReport
    {
        public Guid Id { get; set; }

        public ReportReason Reason { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public Guid PostId { get; set; }
        public Guid ReportedByUserId { get; set; }
        public Guid? ReviewedByUserId { get; set; }

        [JsonIgnore]
        public Post Post { get; set; } = null!;

        [JsonIgnore]
        public User ReportedBy { get; set; } = null!;

        [JsonIgnore]
        public User? ReviewedBy { get; set; }
    }
}