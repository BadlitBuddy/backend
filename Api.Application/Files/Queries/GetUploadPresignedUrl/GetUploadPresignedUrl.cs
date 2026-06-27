using Api.Application.Common.Interfaces;
using Shared.Abstractions.ExternalServices.S3;
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
        Guard.Against.NullOrWhiteSpace(_currentUser.Id, nameof(_currentUser.Id));

        try
        {
            var presignedUrlResult = await _audioJobStorageService.CreateUploadUrlAsync(_currentUser.Id,
                request.FileName);
            return Result<UploadUrlDto>.Success(presignedUrlResult);
        }
        catch (Exception)
        {
            return Result<UploadUrlDto>.Failure(["Could not generate  upload url."]);
        }
    }
}