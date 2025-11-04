using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.DTOs.Forum
{
   
    public class ForumCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int PostCount { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }

    public class CreateForumCategoryDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? Icon { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateForumCategoryDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? Icon { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class PostDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public List<string> Tags { get; set; } = new();
        public PostStatus Status { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int ReactionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }

        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;

        public bool HasUserReacted { get; set; }
        public ReactionType? UserReactionType { get; set; }
    }

    public class PostSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentPreview { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int ReactionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActivityAt { get; set; }

        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class CreatePostDto
    {
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MinLength(10)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public Guid CategoryId { get; set; }

        public string? ImageUrl { get; set; }

        public string? VideoUrl { get; set; }

        public List<string> Tags { get; set; } = new();
    }

    public class UpdatePostDto
    {
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MinLength(10)]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? VideoUrl { get; set; }

        public List<string> Tags { get; set; } = new();
    }

    public class CommentDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        public Guid PostId { get; set; }
        public Guid? ParentCommentId { get; set; }

        public int ReactionCount { get; set; }
        public int ReplyCount { get; set; }

        public bool HasUserReacted { get; set; }
        public ReactionType? UserReactionType { get; set; }

        public List<CommentDto> Replies { get; set; } = new();
    }

    public class CreateCommentDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public Guid PostId { get; set; }

        public Guid? ParentCommentId { get; set; }

        public string? ImageUrl { get; set; }
    }

    public class UpdateCommentDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
    }

    public class AddReactionDto
    {
        [Required]
        public ReactionType Type { get; set; }
    }

    public class ReactionSummaryDto
    {
        public ReactionType Type { get; set; }
        public int Count { get; set; }
        public bool UserReacted { get; set; }
    }

    public class CreateReportDto
    {
        [Required]
        public Guid PostId { get; set; }

        [Required]
        public ReportReason Reason { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    public class ReportDto
    {
        public Guid Id { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
        public ReportStatus Status { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public Guid PostId { get; set; }
        public string PostTitle { get; set; } = string.Empty;

        public Guid ReportedByUserId { get; set; }
        public string ReportedByUsername { get; set; } = string.Empty;

        public Guid? ReviewedByUserId { get; set; }
        public string? ReviewedByUsername { get; set; }
    }

    public class ReviewReportDto
    {
        [Required]
        public ReportStatus Status { get; set; }

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }

    public class PostQueryParameters
    {
        public Guid? CategoryId { get; set; }
        public string? SearchTerm { get; set; }
        public string? Tag { get; set; }
        public PostStatus? Status { get; set; }
        public bool? IsPinned { get; set; }
        public string SortBy { get; set; } = "recent"; // recent, popular, mostCommented
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}