import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ApiService } from './api.service';
import {
    ApiClient,
    EmailLoginRequest,
    UpdateProfileRequest,
    CreateMessageRequest,
    CreateIndividualChatRequest,
    GenerateLoginCodeRequest,
    StartOutgoingCallRequest,
    HandleIncomingCallRequest,
    LogConnectionEstablishedRequest,
    LogRtcErrorRequest,
    LoginResponseDto,
    ChatDto,
    AccountDto,
    MessageDto,
    ProfileDto,
    GenerateLoginCodeResponseDto,
    CallSettingsDto,
    StartOutgoingCallDto,
    RtcErrorType,
} from '../api-client/api-client';
import { UploadImageResponse } from '../protos/file_upload_pb';
import { of } from 'rxjs';
import { ConnectionDiagnostics } from '../models/connection-diagnostics';

describe('ApiService', () => {
    let service: ApiService;
    let apiClient: MockedObject<ApiClient>;

    const mockMessage = new MessageDto({
        id: '1',
        text: 'Hello',
    });

    const mockMessages = [mockMessage];

    const mockProfile = new ProfileDto({
        firstName: 'John',
        lastName: 'Doe',
    });

    const mockChat = new ChatDto({
        id: '1',
        chatName: 'Chat 1',
    });

    const mockChats = [mockChat];

    beforeEach(() => {
        apiClient = {
            emailLogin: vi.fn().mockName('ApiClient.emailLogin'),
            chatAll: vi.fn().mockName('ApiClient.chatAll'),
            account: vi.fn().mockName('ApiClient.account'),
            messageAll: vi.fn().mockName('ApiClient.messageAll'),
            profileGET: vi.fn().mockName('ApiClient.profileGET'),
            profilePUT: vi.fn().mockName('ApiClient.profilePUT'),
            message: vi.fn().mockName('ApiClient.message'),
            chat: vi.fn().mockName('ApiClient.chat'),
            markAsRead: vi.fn().mockName('ApiClient.markAsRead'),
            generateLoginCode: vi.fn().mockName('ApiClient.generateLoginCode'),
            startOutgoingCall: vi.fn().mockName('ApiClient.startOutgoingCall'),
            handleIncomingCall: vi
                .fn()
                .mockName('ApiClient.handleIncomingCall'),
            logConnectionEstablished: vi
                .fn()
                .mockName('ApiClient.logConnectionEstablished'),
            logRtcError: vi.fn().mockName('ApiClient.logRtcError'),
            callSettings: vi.fn().mockName('ApiClient.callSettings'),
        } as MockedObject<ApiClient>;

        TestBed.configureTestingModule({
            providers: [
                ApiService,
                { provide: ApiClient, useValue: apiClient },
            ],
        });

        service = TestBed.inject(ApiService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should login by email', async () => {
        // Arrange
        const mockResponse = new LoginResponseDto({ success: true });
        apiClient.emailLogin.mockReturnValue(of(mockResponse));

        // Act
        const result = await service.loginByEmail('test@example.com', 123456);

        // Assert
        expect(apiClient.emailLogin).toHaveBeenCalledWith(
            expect.any(EmailLoginRequest),
        );
        expect(result).toEqual(mockResponse);
    });

    it('should get chats', async () => {
        // Arrange
        apiClient.chatAll.mockReturnValue(of(mockChats));

        // Act
        const result = await service.getChats();

        // Assert
        expect(apiClient.chatAll).toHaveBeenCalled();
        expect(result).toEqual(mockChats);
    });

    it('should get accounts', async () => {
        // Arrange
        const mockAccounts = [
            new AccountDto({
                id: '1',
                firstName: 'John',
            }),
        ];
        apiClient.account.mockReturnValue(of(mockAccounts));

        // Act
        const result = await service.getAccounts();

        // Assert
        expect(apiClient.account).toHaveBeenCalled();
        expect(result).toEqual(mockAccounts);
    });

    it('should get messages', async () => {
        // Arrange
        apiClient.messageAll.mockReturnValue(of(mockMessages));

        // Act
        const result = await service.getMessages('chatId', 0);

        // Assert
        expect(apiClient.messageAll).toHaveBeenCalledWith('chatId', undefined);
        expect(result).toEqual(mockMessages);
    });

    it('should get profile', async () => {
        // Arrange
        apiClient.profileGET.mockReturnValue(of(mockProfile));

        // Act
        const result = await service.getProfile();

        // Assert
        expect(apiClient.profileGET).toHaveBeenCalled();
        expect(result).toEqual(mockProfile);
    });

    it('should save profile', async () => {
        // Arrange
        const mockImage = new UploadImageResponse();
        mockImage.setId('imageId');
        mockImage.setWidth(100);
        mockImage.setHeight(100);
        mockImage.setImageFormat(1);
        mockImage.setSignature('signature');

        apiClient.profilePUT.mockReturnValue(of(mockProfile));

        // Act
        const result = await service.saveProfile('John', 'Doe', mockImage);

        // Assert
        expect(apiClient.profilePUT).toHaveBeenCalledWith(
            expect.any(UpdateProfileRequest),
        );
        expect(result).toEqual(mockProfile);
    });

    it('should send a message', async () => {
        // Arrange
        apiClient.message.mockReturnValue(of(mockMessage));

        // Act
        const result = await service.sendMessage('chatId', 'Hello');

        // Assert
        expect(apiClient.message).toHaveBeenCalledWith(
            expect.any(CreateMessageRequest),
        );
        expect(result).toEqual(mockMessage);
    });

    it('should create individual chat', async () => {
        // Arrange
        apiClient.chat.mockReturnValue(of(mockChat));

        // Act
        const result = await service.createIndividualChat('accountId');

        // Assert
        expect(apiClient.chat).toHaveBeenCalledWith(
            expect.any(CreateIndividualChatRequest),
        );
        expect(result).toEqual(mockChat);
    });

    it('should mark message as read', async () => {
        // Arrange
        apiClient.markAsRead.mockReturnValue(of(undefined));

        // Act
        const result = await service.markAsRead('chatId', 'messageId');

        // Assert
        expect(apiClient.markAsRead).toHaveBeenCalledWith(
            'chatId',
            'messageId',
        );
        expect(result).toBeUndefined();
    });

    it('should generate login code', async () => {
        // Arrange
        const mockResponse = new GenerateLoginCodeResponseDto();
        apiClient.generateLoginCode.mockReturnValue(of(mockResponse));

        // Act
        const result = await service.generateLoginCode('test@example.com');

        // Assert
        expect(apiClient.generateLoginCode).toHaveBeenCalledWith(
            expect.any(GenerateLoginCodeRequest),
        );
        expect(result).toEqual(mockResponse);
    });

    it('should start outgoing call', async () => {
        // Arrange
        const mockResponse = new StartOutgoingCallDto();
        const mockDiagnostics: ConnectionDiagnostics = {
            connectionState: 'connected',
            localCandidateTypes: { host: 1 },
            remoteCandidateTypes: { srflx: 2 },
            browser: 'Chrome',
            platform: 'Win32',
        };
        apiClient.startOutgoingCall.mockReturnValue(of(mockResponse));

        // Act
        const result = await service.startOutgoingCall(
            'chatId',
            'offer-sdp',
            1500,
            true,
            mockDiagnostics,
        );

        // Assert
        expect(apiClient.startOutgoingCall).toHaveBeenCalledWith(
            expect.any(StartOutgoingCallRequest),
        );
        expect(result).toEqual(mockResponse);
    });

    it('should handle incoming call', async () => {
        // Arrange
        const mockDiagnostics: ConnectionDiagnostics = {
            connectionState: 'connected',
            localCandidateTypes: { host: 1 },
            remoteCandidateTypes: { srflx: 2 },
            browser: 'Chrome',
            platform: 'Win32',
        };
        apiClient.handleIncomingCall.mockReturnValue(of(undefined));

        // Act
        const result = await service.handleIncomingCall(
            'callId',
            'chatId',
            'answer-sdp',
            1500,
            true,
            mockDiagnostics,
        );

        // Assert
        expect(apiClient.handleIncomingCall).toHaveBeenCalledWith(
            expect.any(HandleIncomingCallRequest),
        );
        expect(result).toBeUndefined();
    });

    it('should log connection established', async () => {
        // Arrange
        const mockDiagnostics: ConnectionDiagnostics = {
            connectionState: 'connected',
            localCandidateTypes: { host: 1 },
            remoteCandidateTypes: { srflx: 2 },
            browser: 'Chrome',
            platform: 'Win32',
        };
        apiClient.logConnectionEstablished.mockReturnValue(of(undefined));

        // Act
        const result = await service.logConnectionEstablished(
            'callId',
            'chatId',
            mockDiagnostics,
        );

        // Assert
        expect(apiClient.logConnectionEstablished).toHaveBeenCalledWith(
            expect.any(LogConnectionEstablishedRequest),
        );
        expect(result).toBeUndefined();
    });

    it('should log WebRTC error', async () => {
        // Arrange
        const mockDiagnostics: ConnectionDiagnostics = {
            connectionState: 'failed',
            localCandidateTypes: { host: 1 },
            remoteCandidateTypes: {},
            browser: 'Chrome',
            platform: 'Win32',
        };
        const mockError = new Error('ICE failed');
        apiClient.logRtcError.mockReturnValue(of(undefined));

        // Act
        const result = await service.logWebRtcError(
            'callId',
            'chatId',
            mockDiagnostics,
            RtcErrorType.IceServer,
            mockError,
            mockError.stack,
        );

        // Assert
        expect(apiClient.logRtcError).toHaveBeenCalledWith(
            expect.any(LogRtcErrorRequest),
        );
        expect(result).toBeUndefined();
    });

    it('should get call settings', async () => {
        // Arrange
        const mockCallSettings = new CallSettingsDto();
        apiClient.callSettings.mockReturnValue(of(mockCallSettings));

        // Act
        const result = await service.getCallSettings();

        // Assert
        expect(apiClient.callSettings).toHaveBeenCalled();
        expect(result).toEqual(mockCallSettings);
    });
});
