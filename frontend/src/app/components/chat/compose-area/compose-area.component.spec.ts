import {
    vi,
    type MockedObject,
    describe,
    it,
    beforeEach,
    expect,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ComposeAreaComponent } from './compose-area.component';
import {
    DefaultProjectorFn,
    MemoizedSelector,
    Store,
    StoreModule,
} from '@ngrx/store';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../services/api.service';
import { ErrorService } from '../../../services/error.service';
import { StoreService } from '../../../services/store.service';
import { IdGeneratorService } from '../../../services/id-generator.service';
import { ImageUploadService } from '../../../services/image-upload.service';
import { UploadImageResponse } from '../../../protos/file_upload_pb';
import { selectSelectedChat } from '../../../state/selected-chat/selected-chat.selector';
import {
    IChatDto,
    ImageDto,
    IMessageDto,
} from '../../../api-client/api-client';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { SendMessageButtonStubComponent } from '../send-button/send-button.component.stub';
import { SelectImageButtonStubComponent } from '../select-image-button/select-image-button.component.stub';
import { errorMessages } from '../../../constants/errors';
import { AutoResizeTextAreaStubComponent } from '../auto-resize-text-area/auto-resize-text-area.component.stub';
import { Router } from '@angular/router';

describe(ComposeAreaComponent.name, () => {
    let component: ComposeAreaComponent;
    let fixture: ComponentFixture<ComposeAreaComponent>;
    let apiService: MockedObject<ApiService>;
    let errorService: MockedObject<ErrorService>;
    let imageUploadService: MockedObject<ImageUploadService>;
    let storeService: MockedObject<StoreService>;
    let idGeneratorService: MockedObject<IdGeneratorService>;
    let router: MockedObject<Router>;
    let store: MockStore;
    let mockSelectSelectedChat: MemoizedSelector<
        object,
        IChatDto,
        DefaultProjectorFn<IChatDto>
    >;

    const mockChat: IChatDto = {
        id: '123',
        accountTypeId: 1,
        chatName: 'Test Chat',
        unreadCount: 0,
        lastMessageDate: 12345,
        lastMessageId: '1',
        image: new ImageDto({
            id: 'image123',
            fileStorageTypeId: 1,
        }),
        isIndividual: false,
        accountIds: ['account1'],
    };

    beforeEach(async () => {
        apiService = {
            sendMessage: vi.fn().mockName('ApiService.sendMessage'),
            createIndividualChat: vi
                .fn()
                .mockName('ApiService.createIndividualChat'),
        } as MockedObject<ApiService>;
        errorService = {
            handleError: vi.fn().mockName('ErrorService.handleError'),
        } as MockedObject<ErrorService>;
        storeService = {
            addMessage: vi.fn().mockName('StoreService.addMessage'),
            setLastMessageInfo: vi
                .fn()
                .mockName('StoreService.setLastMessageInfo'),
            updateChatId: vi.fn().mockName('StoreService.updateChatId'),
        } as MockedObject<StoreService>;
        idGeneratorService = {
            isFake: vi.fn().mockName('IdGeneratorService.isFake'),
        } as MockedObject<IdGeneratorService>;
        imageUploadService = {
            resizeAndUploadImage: vi
                .fn()
                .mockName('ImageUploadService.resizeAndUploadImage'),
        } as MockedObject<ImageUploadService>;
        router = {
            navigate: vi.fn().mockName('Router.navigate'),
        } as MockedObject<Router>;
        router.navigate.mockResolvedValue(true);

        await TestBed.configureTestingModule({
            declarations: [
                ComposeAreaComponent,
                SendMessageButtonStubComponent,
                SelectImageButtonStubComponent,
                AutoResizeTextAreaStubComponent,
            ],
            imports: [FormsModule, StoreModule.forRoot({})],
            providers: [
                provideMockStore({}),
                { provide: ApiService, useValue: apiService },
                { provide: ErrorService, useValue: errorService },
                { provide: StoreService, useValue: storeService },
                {
                    provide: IdGeneratorService,
                    useValue: idGeneratorService,
                },
                {
                    provide: ImageUploadService,
                    useValue: imageUploadService,
                },
                { provide: Router, useValue: router },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ComposeAreaComponent);
        component = fixture.componentInstance;
        store = TestBed.inject(Store) as MockStore;
        mockSelectSelectedChat = store.overrideSelector(
            selectSelectedChat,
            null,
        );
    });

    it('should send a message and reset the message field', async () => {
        // Arrange
        mockSelectSelectedChat.setResult(mockChat);
        const message = 'Hello, world!';
        component.message.set(message);
        const messageDto = {
            created: 1728849011,
            id: '234',
        } as IMessageDto;
        apiService.sendMessage.mockResolvedValue(messageDto);

        // Act
        store.refreshState();
        fixture.detectChanges();
        await component.onSendMessage(message);

        // Assert
        expect(apiService.sendMessage).toHaveBeenCalledWith(
            mockChat.id,
            message,
            undefined,
        );
        expect(storeService.addMessage).toHaveBeenCalledTimes(1);
        expect(storeService.addMessage).toHaveBeenCalledWith(messageDto);
        expect(storeService.setLastMessageInfo).toHaveBeenCalledTimes(1);
        expect(storeService.setLastMessageInfo).toHaveBeenCalledWith(
            mockChat.id,
            messageDto.created,
            messageDto.id,
        );
        expect(errorService.handleError).not.toHaveBeenCalled();
        expect(apiService.createIndividualChat).not.toHaveBeenCalled();
        expect(component.message()).toBe('');
        expect(component.isSending()).toBe(false);
    });

    it('should handle image blob and send a message with image', async () => {
        // Arrange
        mockSelectSelectedChat.setResult(mockChat);
        const blob = new Blob(['image-data']);
        const mockImageResponse = new UploadImageResponse();
        imageUploadService.resizeAndUploadImage.mockResolvedValue(
            mockImageResponse,
        );
        const messageDto = {
            created: 1728849011,
            id: '234',
        } as IMessageDto;
        apiService.sendMessage.mockResolvedValue(messageDto);

        const revokeObjectURLSpy = vi.spyOn(URL, 'revokeObjectURL');

        // Act
        store.refreshState();
        fixture.detectChanges();
        await component.onImageBlobReady(blob);

        // Assert
        expect(imageUploadService.resizeAndUploadImage).toHaveBeenCalled();
        expect(apiService.sendMessage).toHaveBeenCalledWith(
            mockChat.id,
            undefined,
            mockImageResponse,
        );
        expect(storeService.addMessage).toHaveBeenCalledTimes(1);
        expect(storeService.addMessage).toHaveBeenCalledWith(messageDto);
        expect(storeService.setLastMessageInfo).toHaveBeenCalledTimes(1);
        expect(storeService.setLastMessageInfo).toHaveBeenCalledWith(
            mockChat.id,
            messageDto.created,
            messageDto.id,
        );
        expect(apiService.createIndividualChat).not.toHaveBeenCalled();
        expect(errorService.handleError).not.toHaveBeenCalled();
        expect(apiService.createIndividualChat).not.toHaveBeenCalled();
        expect(revokeObjectURLSpy).toHaveBeenCalled();
    });

    it('should handle errors during message sending', async () => {
        // Arrange
        mockSelectSelectedChat.setResult(mockChat);
        const error = new Error('Test Error');
        apiService.sendMessage.mockImplementation(() => {
            throw error;
        });

        // Act
        store.refreshState();
        fixture.detectChanges();
        await component.onSendMessage('test');

        // Assert
        expect(errorService.handleError).toHaveBeenCalledTimes(1);

        // Assert
        expect(errorService.handleError).toHaveBeenCalledWith(
            error,
            errorMessages.sendMessage,
        );
        expect(storeService.addMessage).not.toHaveBeenCalled();
        expect(storeService.setLastMessageInfo).not.toHaveBeenCalled();
        expect(apiService.createIndividualChat).not.toHaveBeenCalled();
        expect(component.isSending()).toBe(false);
    });

    it('should create an individual chat if needed and send a message', async () => {
        // Arrange
        const individualChat = { ...mockChat, isIndividual: true };
        mockSelectSelectedChat.setResult(individualChat);
        idGeneratorService.isFake.mockReturnValue(true);

        const createdChat = { ...individualChat, id: 'real-chat-id' };
        apiService.createIndividualChat.mockResolvedValue(createdChat);

        const messageDto = {
            created: 1728849011,
            id: '234',
        } as IMessageDto;
        apiService.sendMessage.mockResolvedValue(messageDto);

        // Act
        store.refreshState();
        fixture.detectChanges();
        await component.onSendMessage('Hello');

        // Assert
        expect(apiService.createIndividualChat).toHaveBeenCalledWith(
            individualChat.accountIds[0],
        );
        expect(storeService.updateChatId).toHaveBeenCalledWith(
            individualChat.id,
            createdChat.id,
        );
        expect(router.navigate).toHaveBeenCalledWith([
            '/messenger/chat',
            createdChat.id,
        ]);
        expect(apiService.sendMessage).toHaveBeenCalledWith(
            createdChat.id,
            'Hello',
            undefined,
        );
        expect(errorService.handleError).not.toHaveBeenCalled();
        expect(storeService.addMessage).not.toHaveBeenCalled();
        expect(storeService.setLastMessageInfo).not.toHaveBeenCalled();
    });

    it('should unsubscribe from store on destroy', () => {
        // Arrange
        const unsubscribeSpy = vi.spyOn(component['unsubscribe$'], 'next');
        const completeSpy = vi.spyOn(component['unsubscribe$'], 'complete');

        // Act
        component.ngOnDestroy();

        // Assert
        expect(unsubscribeSpy).toHaveBeenCalled();
        expect(completeSpy).toHaveBeenCalled();
    });
});
