import * as grpcWeb from 'grpc-web';

import * as file_upload_pb from './file_upload_pb'; // proto import: "file_upload.proto"


export class FileUploadGrpcEndpointClient {
  constructor (hostname: string,
               credentials?: null | { [index: string]: string; },
               options?: null | { [index: string]: any; });

  uploadImageAsync(
    request: file_upload_pb.UploadImageRequest,
    metadata: grpcWeb.Metadata | undefined,
    callback: (err: grpcWeb.RpcError,
               response: file_upload_pb.UploadImageResponse) => void
  ): grpcWeb.ClientReadableStream<file_upload_pb.UploadImageResponse>;

  downloadImageAsync(
    request: file_upload_pb.DownloadImageRequest,
    metadata: grpcWeb.Metadata | undefined,
    callback: (err: grpcWeb.RpcError,
               response: file_upload_pb.DownloadImageResponse) => void
  ): grpcWeb.ClientReadableStream<file_upload_pb.DownloadImageResponse>;

}

export class FileUploadGrpcEndpointPromiseClient {
  constructor (hostname: string,
               credentials?: null | { [index: string]: string; },
               options?: null | { [index: string]: any; });

  uploadImageAsync(
    request: file_upload_pb.UploadImageRequest,
    metadata?: grpcWeb.Metadata
  ): Promise<file_upload_pb.UploadImageResponse>;

  downloadImageAsync(
    request: file_upload_pb.DownloadImageRequest,
    metadata?: grpcWeb.Metadata
  ): Promise<file_upload_pb.DownloadImageResponse>;

}

