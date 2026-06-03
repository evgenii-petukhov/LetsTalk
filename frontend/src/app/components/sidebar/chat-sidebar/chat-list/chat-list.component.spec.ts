import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ChatListComponent } from './chat-list.component';
import { DefaultProjectorFn, MemoizedSelector, Store } from '@ngrx/store';
import { StoreService } from '../../../../services/store.service';
import { IdGeneratorService } from '../../../../services/id-generator.service';
import { IChatDto } from '../../../../api-client/api-client';
import { OrderByPipe } from '../../../../pipes/orderby';
import { ChatListItemStubComponent } from '../chat-list-item/chat-list-item.component.stub';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { selectChats } from '../../../../state/chats/chats.selector';
import { selectSelectedChatId } from '../../../../state/selected-chat/selected-chat-info.selectors';
import { By } from '@angular/platform-browser';

describe('ChatListComponent', () => {
    let component: ChatListComponent;
    let fixture: ComponentFixture<ChatListComponent>;
    let store: MockStore;
    let storeService: MockedObject<StoreService>;
    let idGeneratorService: MockedObject<IdGeneratorService>;
    let mockSelectSelectedChatId: MemoizedSelector<
        object,
        string,
        DefaultProjectorFn<string>
    >;
    let mockSelectChats: MemoizedSelector<
        object,
        readonly IChatDto[],
        DefaultProjectorFn<readonly IChatDto[]>
    >;

    beforeEach(async () => {
        storeService = {
            setSelectedChatId: vi
                .fn()
                .mockName('StoreService.setSelectedChatId'),
            markAllAsRead: vi.fn().mockName('StoreService.markAllAsRead'),
        } as MockedObject<StoreService>;
        idGeneratorService = {
            isFake: vi.fn().mockName('IdGeneratorService.isFake'),
        } as MockedObject<IdGeneratorService>;

        await TestBed.configureTestingModule({
            declarations: [
                ChatListComponent,
                ChatListItemStubComponent,
                OrderByPipe,
            ],
            providers: [
                provideMockStore({}),
                { provide: StoreService, useValue: storeService },
                { provide: IdGeneratorService, useValue: idGeneratorService },
            ],
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(ChatListComponent);
        component = fixture.componentInstance;
        store = TestBed.inject(Store) as MockStore;
        mockSelectChats = store.overrideSelector(selectChats, null);
        mockSelectSelectedChatId = store.overrideSelector(
            selectSelectedChatId,
            null,
        );
    });

    it('should create the component', () => {
        expect(component).toBeTruthy();
    });

    it('should output ChatListItemComponent instances sorted by chatName', () => {
        // Arrange
        const chat1: IChatDto = {
            id: 'chat1',
            chatName: 'Neil Johnston',
        };

        const chat2: IChatDto = {
            id: 'chat2',
            chatName: 'Bob Pettit',
        };

        const chat3: IChatDto = {
            id: 'chat3',
            chatName: 'Rick Barry',
        };

        const chat4: IChatDto = {
            id: 'chat4',
            chatName: 'George Gevin',
        };

        const selectedChatId = 'chat3';
        mockSelectChats.setResult([chat1, chat2, chat3, chat4]);
        mockSelectSelectedChatId.setResult(selectedChatId);

        // Act
        store.refreshState();
        fixture.detectChanges();

        // Assert
        const chatListItems = fixture.debugElement
            .queryAll(By.directive(ChatListItemStubComponent))
            .map((element) => element.componentInstance);

        expect(chatListItems.length).toBe(4);
        expect(chatListItems[0].chat).toBe(chat2);
        expect(chatListItems[1].chat).toBe(chat4);
        expect(chatListItems[2].chat).toBe(chat1);
        expect(chatListItems[3].chat).toBe(chat3);

        const selected = fixture.debugElement
            .query(By.css('li.selected'))
            .query(By.directive(ChatListItemStubComponent));

        expect(selected.componentInstance.chat).toBe(chat3);
    });

    it('should output ChatListItemComponent sorted by unreadCount (desc) then by chatName', () => {
        // Arrange
        const chat1: IChatDto = {
            id: 'chat1',
            chatName: 'Neil Johnston',
            unreadCount: 7,
        };

        const chat2: IChatDto = {
            id: 'chat2',
            chatName: 'Bob Pettit',
            unreadCount: 3,
        };

        const chat3: IChatDto = {
            id: 'chat3',
            chatName: 'Rick Barry',
            unreadCount: 10,
        };

        const chat4: IChatDto = {
            id: 'chat4',
            chatName: 'George Gevin',
            unreadCount: 6,
        };

        const selectedChatId = 'chat2';
        mockSelectChats.setResult([chat1, chat2, chat3, chat4]);
        mockSelectSelectedChatId.setResult(selectedChatId);

        // Act
        store.refreshState();
        fixture.detectChanges();

        // Assert
        const chatListItems = fixture.debugElement
            .queryAll(By.directive(ChatListItemStubComponent))
            .map((element) => element.componentInstance);

        expect(chatListItems.length).toBe(4);
        expect(chatListItems[0].chat).toBe(chat3);
        expect(chatListItems[1].chat).toBe(chat1);
        expect(chatListItems[2].chat).toBe(chat4);
        expect(chatListItems[3].chat).toBe(chat2);

        const selected = fixture.debugElement
            .query(By.css('li.selected'))
            .query(By.directive(ChatListItemStubComponent));

        expect(selected.componentInstance.chat).toBe(chat2);
    });

    it('should output  ChatListItemComponent sorted by lastMessageDate (desc), then by chatName', () => {
        // Arrange
        const chat1: IChatDto = {
            id: 'chat1',
            chatName: 'Neil Johnston',
            lastMessageDate: 1725039342,
        };

        const chat2: IChatDto = {
            id: 'chat2',
            chatName: 'Bob Pettit',
            lastMessageDate: 1724866542,
        };

        const chat3: IChatDto = {
            id: 'chat3',
            chatName: 'Rick Barry',
            lastMessageDate: 1724952942,
        };

        const chat4: IChatDto = {
            id: 'chat4',
            chatName: 'George Gevin',
            lastMessageDate: 1725132982,
        };

        const selectedChatId = 'chat1';
        mockSelectChats.setResult([chat1, chat2, chat3, chat4]);
        mockSelectSelectedChatId.setResult(selectedChatId);

        // Act
        store.refreshState();
        fixture.detectChanges();

        // Assert
        const chatListItems = fixture.debugElement
            .queryAll(By.directive(ChatListItemStubComponent))
            .map((element) => element.componentInstance);

        expect(chatListItems.length).toBe(4);
        expect(chatListItems[0].chat).toBe(chat4);
        expect(chatListItems[1].chat).toBe(chat1);
        expect(chatListItems[2].chat).toBe(chat3);
        expect(chatListItems[3].chat).toBe(chat2);

        const selected = fixture.debugElement
            .query(By.css('li.selected'))
            .query(By.directive(ChatListItemStubComponent));

        expect(selected.componentInstance.chat).toBe(chat1);
    });
});
