using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Users;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\UserGoal\UserGoalController</c> — Kullanıcılar formunun
/// "Hedefler" sekmesi. Kaynakta yetki kontrolü YOK/YORUMDA (bkz.
/// UserGoalService üstündeki not) — burada <c>user_management</c> altında
/// gerçek CRUD yetkisi uygulanıyor (formun geri kalanıyla aynı sayfa).
/// </summary>
[Authorize]
[Route("api/v1/user_goal")]
public sealed class UserGoalController : ApiControllerBase
{
    private const string PermissionSlug = "user_management";

    private readonly IUserGoalService _goals;

    public UserGoalController(IUserGoalService goals)
    {
        _goals = goals;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, PermissionSlug)]
    public async Task<IActionResult> Index(
        [FromQuery(Name = "user_id")] int? userId, CancellationToken cancellationToken)
    {
        if (userId is null or <= 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["user_id"] = [Translator.Get("Zorunlu Alan")] }));

        var result = await _goals.ListAsync(userId.Value, cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, PermissionSlug)]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var goal = await _goals.SingleAsync(id, cancellationToken);

        return goal is null ? NotFoundError() : Ok(goal, "Kayıtlar");
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, PermissionSlug)]
    public async Task<IActionResult> Save(
        [FromBody] UserGoalRequest request, CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var result = await _goals.CreateAsync(request.ToInput(), cancellationToken);

        return result.Success
            ? Ok(result.Data!, "Kayıt Başarılı")
            : BadRequest(ApiResponse.Message(result.Error!));
    }

    [HttpPut]
    [RequiresPermission(PermissionAction.Update, PermissionSlug)]
    public async Task<IActionResult> Update(
        [FromBody] UserGoalUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.Id is null or <= 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["id"] = [Translator.Get("Zorunlu Alan")] }));

        var errors = Validate(request);
        if (errors.Count > 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var result = await _goals.UpdateAsync(request.Id.Value, request.ToInput(), cancellationToken);

        return result.Success
            ? Ok(result.Data!, "Güncelleme Başarılı")
            : BadRequest(ApiResponse.Message(result.Error!));
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, PermissionSlug)]
    public async Task<IActionResult> Delete(
        [FromBody] InvoiceController.DeleteRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId is not { Count: > 0 })
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["deletion_id"] = [Translator.Get("Zorunlu Alan")] }));

        await _goals.DeleteAsync(request.DeletionId, cancellationToken);

        return base.Ok(ApiResponse.Message("Kayıt Başarıyla Silindi"));
    }

    private Dictionary<string, string[]> Validate(UserGoalRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var required = Translator.Get("Zorunlu Alan");

        if (request.UserId is null or <= 0)
            errors["user_id"] = [required];

        if (request.GoalPrice is null)
            errors["goal_price"] = [required];

        return errors;
    }

    public class UserGoalRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("start_date")]
        public DateOnly? StartDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("end_date")]
        public DateOnly? EndDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("goal_price")]
        public decimal? GoalPrice { get; set; }

        public UserGoalInput ToInput() => new()
        {
            UserId = UserId ?? 0,
            StartDate = StartDate,
            EndDate = EndDate,
            GoalPrice = GoalPrice ?? 0m,
        };
    }

    public sealed class UserGoalUpdateRequest : UserGoalRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public long? Id { get; set; }
    }
}
