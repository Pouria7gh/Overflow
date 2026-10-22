using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.DTOs;
using QuestionService.Models;

namespace QuestionService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext db) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Question>> CreateQuestion(CreateQuestionDto dto)
    {
        // validates tags
        var tags = await db.Tags.Where(x => dto.Tags.Contains(x.Slug)).ToListAsync();
        var missing = dto.Tags.Except(tags.Select(x => x.Slug).ToList()).ToList();
        if (missing.Count > 0)
        {
            return BadRequest($"Invalid tags: {string.Join(", ", missing)}");
        }
        
        // validates user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue("name");
        if (userId is null || name is null) return BadRequest("Cannot Get User Details");

        // adds question to db
        var question = new Question()
        {
            AskerId = userId,
            AskerDisplayName = name,
            Title = dto.Title,
            Content = dto.Content,
            TagSlugs = dto.Tags,
        };
        db.Questions.Add(question); 
        await db.SaveChangesAsync();
        
        return Created($"questions/{question.Id}", question);
    }

    [HttpGet]
    public async Task<ActionResult<List<Question>>> GetQuestions(string? tag)
    {
        var query = db.Questions.AsQueryable();

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(q => q.TagSlugs.Contains(tag));
        }

        return await query.OrderBy(q => q.CreatedAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Question>> GetQuestion(string id)
    {
        var question = await db.Questions.FindAsync(id);

        if (question is null)
        {
            return NotFound($"Question with id {id} not found");
        }

        await db.Questions.Where(x => x.Id == id).ExecuteUpdateAsync(setters =>
            setters.SetProperty(q => q.ViewCount, q => q.ViewCount + 1));

        return question;
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuestion(string id, CreateQuestionDto dto)
    {
        // validate question id
        var question = await db.Questions.FindAsync(id);
        if (question is null)
        {
            return NotFound("Question not found");
        }

        // validate user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("Cannot Get User Details");
        }

        // make sure user is asker
        if (question.AskerId != userId)
        {
            return Forbid();
        }
        
        // validates tags
        var tags = await db.Tags.Where(x => dto.Tags.Contains(x.Slug)).ToListAsync();
        var missing = dto.Tags.Except(tags.Select(x => x.Slug).ToList()).ToList();
        if (missing.Count > 0)
        {
            return BadRequest($"Invalid tags: {string.Join(", ", missing)}");
        }

        // update question
        question.Title = dto.Title;
        question.Content = dto.Content;
        question.TagSlugs = dto.Tags;
        question.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuestion(string id)
    {
        // validate user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("Cannot Get User Details");
        }

        // validate question
        var question = await db.Questions.FindAsync(id);
        if (question is null)
        {
            return NotFound("Question not found");
        }

        // make sure user is asker
        if (question.AskerId != userId)
        {
            return Forbid();
        }

        // remove question
        db.Questions.Remove(question);
        await db.SaveChangesAsync();

        return NoContent();
    }
}