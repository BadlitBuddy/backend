using Api.Application.Common.Interfaces;
using Shared.Abstractions.Services;
using Shared.Contracts.Dtos;

namespace Api.Application.Files.Queries.GetUploadPresignedUrl;

public class GetUploadPresignedUrlQuery : IRequest<Result<UploadUrlDto>>
{
    public required string FileName { get; set; }
}

public class GetUploadPresignedUrlHandler : IRequestHandler<GetUploadPresignedUrlQuery, Result<UploadUrlDto>>
{
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly IUser _currentUser;

    public GetUploadPresignedUrlHandler(IAudioJobStorageService audioJobStorageService, IUser currentUser)
    {
        _audioJobStorageService = audioJobStorageService;
        _currentUser = currentUser;
    }

    public async Task<Result<UploadUrlDto>> Handle(GetUploadPresignedUrlQuery request,
        CancellationToken cancellationToken)
    {
        List<string> allowedExtensions = [".wav"];

        char[] invalidChars = Path.GetInvalidFileNameChars();

        string fileName = Path.GetFileName(request.FileName);
        string cleanFileName = string.Concat(fileName.Split(invalidChars));
        string fileExtension = Path.GetExtension(cleanFileName);

        if (cleanFileName.Length > 30)
        {
            return Result<UploadUrlDto>.Failure(
                ["File name is too long. Please rename to be under 30 characters long."]);
        }

        if (!allowedExtensions.Contains(fileExtension))
        {
            return Result<UploadUrlDto>.Failure(
                [$"File must be one of the following {string.Join(",", allowedExtensions)}"]);
        }

        var userId = _currentUser.IsAuthenticated ? _currentUser.Id! : "019feb11-ff3d-7fef-8323-1c53f0e3b0da";
        try
        {
            var presignedUrlResult = await _audioJobStorageService.CreateUploadUrlAsync(userId, cleanFileName);
            return Result<UploadUrlDto>.Success(presignedUrlResult);
        }
        catch (Exception)
        {
            return Result<UploadUrlDto>.Failure(["Could not generate  upload url."]);
        }
    }
}
