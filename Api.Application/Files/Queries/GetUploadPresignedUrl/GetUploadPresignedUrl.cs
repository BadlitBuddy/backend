using Api.Application.Common.Interfaces;
using Ardalis.GuardClauses;
using Shared.Abstractions.ExternalServices.S3;
using Shared.Contracts.Dtos;

namespace Api.Application.Files.Queries.GetUploadPresignedUrl;

public class GetUploadPresignedUrlQuery : IRequest<UploadUrlDto>
{
    public required string FileName { get; set; }
}

public class GetUploadPresignedUrlHandler : IRequestHandler<GetUploadPresignedUrlQuery, UploadUrlDto>
{
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly IUser _currentUser;

    public GetUploadPresignedUrlHandler(IAudioJobStorageService audioJobStorageService, IUser currentUser)
    {
        _audioJobStorageService = audioJobStorageService;
        _currentUser = currentUser;
    }

    public async Task<UploadUrlDto> Handle(GetUploadPresignedUrlQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(_currentUser.Id, nameof(_currentUser.Id));

        return await _audioJobStorageService.CreateUploadUrlAsync(_currentUser.Id,
            request.FileName);
    }
}