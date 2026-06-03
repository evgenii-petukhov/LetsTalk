import * as jspb from 'google-protobuf'



export class UploadImageRequest extends jspb.Message {
  getContent(): Uint8Array | string;
  getContent_asU8(): Uint8Array;
  getContent_asB64(): string;
  setContent(value: Uint8Array | string): UploadImageRequest;

  getImageRole(): ImageRoles;
  setImageRole(value: ImageRoles): UploadImageRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): UploadImageRequest.AsObject;
  static toObject(includeInstance: boolean, msg: UploadImageRequest): UploadImageRequest.AsObject;
  static serializeBinaryToWriter(message: UploadImageRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): UploadImageRequest;
  static deserializeBinaryFromReader(message: UploadImageRequest, reader: jspb.BinaryReader): UploadImageRequest;
}

export namespace UploadImageRequest {
  export type AsObject = {
    content: Uint8Array | string;
    imageRole: ImageRoles;
  };
}

export class UploadImageResponse extends jspb.Message {
  getId(): string;
  setId(value: string): UploadImageResponse;

  getWidth(): number;
  setWidth(value: number): UploadImageResponse;

  getHeight(): number;
  setHeight(value: number): UploadImageResponse;

  getImageFormat(): number;
  setImageFormat(value: number): UploadImageResponse;

  getFileStorageTypeId(): number;
  setFileStorageTypeId(value: number): UploadImageResponse;

  getSignature(): string;
  setSignature(value: string): UploadImageResponse;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): UploadImageResponse.AsObject;
  static toObject(includeInstance: boolean, msg: UploadImageResponse): UploadImageResponse.AsObject;
  static serializeBinaryToWriter(message: UploadImageResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): UploadImageResponse;
  static deserializeBinaryFromReader(message: UploadImageResponse, reader: jspb.BinaryReader): UploadImageResponse;
}

export namespace UploadImageResponse {
  export type AsObject = {
    id: string;
    width: number;
    height: number;
    imageFormat: number;
    fileStorageTypeId: number;
    signature: string;
  };
}

export class DownloadImageRequest extends jspb.Message {
  getImageId(): string;
  setImageId(value: string): DownloadImageRequest;

  getFileStorageTypeId(): number;
  setFileStorageTypeId(value: number): DownloadImageRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): DownloadImageRequest.AsObject;
  static toObject(includeInstance: boolean, msg: DownloadImageRequest): DownloadImageRequest.AsObject;
  static serializeBinaryToWriter(message: DownloadImageRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): DownloadImageRequest;
  static deserializeBinaryFromReader(message: DownloadImageRequest, reader: jspb.BinaryReader): DownloadImageRequest;
}

export namespace DownloadImageRequest {
  export type AsObject = {
    imageId: string;
    fileStorageTypeId: number;
  };
}

export class DownloadImageResponse extends jspb.Message {
  getContent(): Uint8Array | string;
  getContent_asU8(): Uint8Array;
  getContent_asB64(): string;
  setContent(value: Uint8Array | string): DownloadImageResponse;

  getWidth(): number;
  setWidth(value: number): DownloadImageResponse;

  getHeight(): number;
  setHeight(value: number): DownloadImageResponse;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): DownloadImageResponse.AsObject;
  static toObject(includeInstance: boolean, msg: DownloadImageResponse): DownloadImageResponse.AsObject;
  static serializeBinaryToWriter(message: DownloadImageResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): DownloadImageResponse;
  static deserializeBinaryFromReader(message: DownloadImageResponse, reader: jspb.BinaryReader): DownloadImageResponse;
}

export namespace DownloadImageResponse {
  export type AsObject = {
    content: Uint8Array | string;
    width: number;
    height: number;
  };
}

export enum ImageRoles {
  UNKNOWN = 0,
  AVATAR = 1,
  MESSAGE = 2,
}
