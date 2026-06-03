import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DefaultProjectorFn, MemoizedSelector, Store } from '@ngrx/store';
import { ChatHeaderComponent } from './chat-header.component';
import { StoreService } from '../../../services/store.service';
import { OrderByPipe } from '../../../pipes/orderby';
import { AvatarStubComponent } from '../../shared/avatar/avatar.component.stub';
import { TopPanelStubComponent } from '../../shared/top-panel/top-panel.component.stub';
import { CallButtonStubComponent } from '../call-button/call-button.component.stub';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { By } from '@angular/platform-browser';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { IChatDto, ImageDto } from '../../../api-client/api-client';
import { selectSelectedChat } from '../../../state/selected-chat/selected-chat.selector';
import { selectIsAnyCallInProgress } from '../../../state/video-call/video-call.selectors';
import { UserDetailsStubComponent } from '../../shared/user-details/user-details.component.stub';
import { ApiService } from '../../../services/api.service';
import { IdGeneratorService } from '../../../services/id-generator.service';
import { Router } from '@angular/router';

describe('ChatHeaderComponent', () => {
    let fixture: ComponentFixture<ChatHeaderComponent>;
    let store: MockStore;
    let storeService: MockedObject<StoreService>;
    let apiService: MockedObject<ApiService>;
    let idGeneratorService: MockedObject<IdGeneratorService>;
    let router: MockedObject<Router>;
    let mockSelectSelectedChat: MemoizedSelector<
        object,
        IChatDto,
        DefaultProjectorFn<IChatDto>
    >;
    let mockSelectIsAnyCallInProgress: MemoizedSelector<
        object,
        boolean,
        DefaultProjectorFn<boolean>
    >;

    const mockChat = {
        id: 'chat1',
        chatName: 'Chat1',
        image: new ImageDto({
            id: 'img1',
            fileStorageTypeId: 1,
        }),
        photoUrl: 'url1',
        isIndividual: false,
        accountIds: ['account1'],
    } as IChatDto;

    const mockChat2 = {
        id: 'chat2',
        chatName: 'Chat2',
        image: new ImageDto({
            id: 'img2',
            fileStorageTypeId: 1,
        }),
        photoUrl: 'url2',
        isIndividual: false,
        accountIds: ['account2'],
    } as IChatDto;

    beforeEach(async () => {
        storeService = {
            setSelectedChatId: vi
                .fn()
                .mockName('StoreService.setSelectedChatId'),
            initOutgoingCall: vi.fn().mockName('StoreService.initOutgoingCall'),
            updateChatId: vi.fn().mockName('StoreService.updateChatId'),
        } as MockedObject<StoreService>;
        apiService = {
            createIndividualChat: vi
                .fn()
                .mockName('ApiService.createIndividualChat'),
        } as MockedObject<ApiService>;
        idGeneratorService = {
            isFake: vi.fn().mockName('IdGeneratorService.isFake'),
        } as MockedObject<IdGeneratorService>;
        router = {
            navigate: vi.fn().mockName('Router.navigate'),
        } as MockedObject<Router>;
        router.navigate.mockResolvedValue(true);

        await TestBed.configureTestingModule({
            declarations: [
                ChatHeaderComponent,
                AvatarStubComponent,
                TopPanelStubComponent,
                CallButtonStubComponent,
                UserDetailsStubComponent,
                OrderByPipe,
            ],
            providers: [
                provideMockStore({}),
                { provide: StoreService, useValue: storeService },
                { provide: ApiService, useValue: apiService },
                { provide: IdGeneratorService, useValue: idGeneratorService },
                { provide: Router, useValue: router },
            ],
            imports: [FontAwesomeModule],
        }).compileComponents();

        fixture = TestBed.createComponent(ChatHeaderComponent);
        store = TestBed.inject(Store) as MockStore;
        mockSelectSelectedChat = store.overrideSelector(
            selectSelectedChat,
            null,
        );
        mockSelectIsAnyCallInProgress = store.overrideSelector(
            selectIsAnyCallInProgress,
            false,
        );
    });

    it('should display the chat name and pass correct urlOptions to app-avatar', () => {
        // Arrange
        mockSelectSelectedChat.setResult(mockChat);

        // Act
        store.refreshState();
        fixture.detectChanges();

        expect(getUserNameElement().value).toContain(mockChat.chatName);

        // Assert
        expect(getAvatarElement().componentInstance.urlOptions).toEqual([
            mockChat.image,
            mockChat.photoUrl,
        ]);

        // Arrange
        mockSelectSelectedChat.setResult(mockChat2);

        // Act
        store.refreshState();
        fixture.detectChanges();

        expect(getUserNameElement().value).toContain(mockChat2.chatName);

        // Assert
        expect(getAvatarElement().componentInstance.urlOptions).toEqual([
            mockChat2.image,
            mockChat2.photoUrl,
        ]);
    });

    it('should call initVideoCall with existing chat id when onCallClicked', async () => {
        // Arrange
        mockSelectSelectedChat.setResult(mockChat);
        store.refreshState();
        fixture.detectChanges();

        // Act
        await fixture.componentInstance.onCallClicked();

        // Assert
        expect(storeService.initOutgoingCall).toHaveBeenCalledWith(mockChat.id);
        expect(apiService.createIndividualChat).not.toHaveBeenCalled();
        expect(storeService.updateChatId).not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should create individual chat and call initVideoCall when chat is individual with fake id', async () => {
        // Arrange
        const individualChat = {
            ...mockChat,
            isIndividual: true,
            id: 'fake-id',
        };
        const createdChat = { ...individualChat, id: 'real-chat-id' };

        mockSelectSelectedChat.setResult(individualChat);
        idGeneratorService.isFake.mockReturnValue(true);
        apiService.createIndividualChat.mockResolvedValue(createdChat);

        store.refreshState();
        fixture.detectChanges();

        // Act
        await fixture.componentInstance.onCallClicked();

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
        expect(storeService.initOutgoingCall).toHaveBeenCalledWith(
            createdChat.id,
        );
    });

    it('should not create individual chat when chat is individual but id is not fake', async () => {
        // Arrange
        const individualChat = { ...mockChat, isIndividual: true };

        mockSelectSelectedChat.setResult(individualChat);
        idGeneratorService.isFake.mockReturnValue(false);

        store.refreshState();
        fixture.detectChanges();

        // Act
        await fixture.componentInstance.onCallClicked();

        // Assert
        expect(apiService.createIndividualChat).not.toHaveBeenCalled();
        expect(storeService.updateChatId).not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalled();
        expect(storeService.initOutgoingCall).toHaveBeenCalledWith(
            individualChat.id,
        );
    });

    it('should handle null chat gracefully when onCallClicked', async () => {
        // Arrange
        mockSelectSelectedChat.setResult(null);
        store.refreshState();
        fixture.detectChanges();

        // Act
        await fixture.componentInstance.onCallClicked();

        // Assert
        expect(storeService.initOutgoingCall).not.toHaveBeenCalled();
        expect(apiService.createIndividualChat).not.toHaveBeenCalled();
    });

    it('should pass backButton input to app-top-panel', () => {
        // Arrange
        fixture.componentInstance.backButton = 'visible';

        // Act
        fixture.detectChanges();

        // Assert
        const topPanel = fixture.debugElement.query(
            By.directive(TopPanelStubComponent),
        );
        expect(topPanel.componentInstance.backButton).toBe('visible');
    });

    it('should not render app-avatar when chat is null', () => {
        // Arrange
        mockSelectSelectedChat.setResult(null);

        // Act
        store.refreshState();
        fixture.detectChanges();

        // Assert
        expect(getAvatarElement()).toBeNull();
    });

    it('should render app-avatar when chat is set', () => {
        // Arrange
        mockSelectSelectedChat.setResult(mockChat);

        // Act
        store.refreshState();
        fixture.detectChanges();

        // Assert
        expect(getAvatarElement()).not.toBeNull();
    });

    it('should hide call button when a call is in progress', () => {
        // Arrange
        mockSelectSelectedChat.setResult(mockChat);
        mockSelectIsAnyCallInProgress.setResult(true);

        // Act
        store.refreshState();
        fixture.detectChanges();

        // Assert
        expect(getCallButtonElement()).toBeNull();
    });

    it('should show call button when no call is in progress', () => {
        // Arrange
        mockSelectSelectedChat.setResult(mockChat);
        mockSelectIsAnyCallInProgress.setResult(false);

        // Act
        store.refreshState();
        fixture.detectChanges();

        // Assert
        expect(getCallButtonElement()).not.toBeNull();
    });

    it('should call onCallClicked when call button emits buttonClick', async () => {
        // Arrange
        mockSelectSelectedChat.setResult(mockChat);
        mockSelectIsAnyCallInProgress.setResult(false);
        store.refreshState();
        fixture.detectChanges();

        const onCallClickedSpy = vi.spyOn(
            fixture.componentInstance,
            'onCallClicked',
        );

        // Act
        getCallButtonElement().componentInstance.buttonClick.emit();
        fixture.detectChanges();

        // Assert
        expect(onCallClickedSpy).toHaveBeenCalled();
    });

    function getAvatarElement() {
        return fixture.debugElement.query(By.directive(AvatarStubComponent));
    }

    function getUserNameElement() {
        return fixture.debugElement.query(
            By.directive(UserDetailsStubComponent),
        ).componentInstance as UserDetailsStubComponent;
    }

    function getCallButtonElement() {
        return fixture.debugElement.query(
            By.directive(CallButtonStubComponent),
        );
    }
});
