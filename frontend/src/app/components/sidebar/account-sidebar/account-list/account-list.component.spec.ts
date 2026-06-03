import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AccountListComponent } from './account-list.component';
import { DefaultProjectorFn, MemoizedSelector, Store } from '@ngrx/store';
import { By } from '@angular/platform-browser';
import { OrderByPipe } from '../../../../pipes/orderby';
import { StoreService } from '../../../../services/store.service';
import { IdGeneratorService } from '../../../../services/id-generator.service';
import {
    IAccountDto,
    IChatDto,
    ImageDto,
} from '../../../../api-client/api-client';
import { AccountListItemStubComponent } from '../account-list-item/account-list-item.component.stub';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { selectChats } from '../../../../state/chats/chats.selector';
import { selectAccounts } from '../../../../state/accounts/accounts.selector';

describe('AccountListComponent', () => {
    let component: AccountListComponent;
    let fixture: ComponentFixture<AccountListComponent>;
    let store: MockStore;
    let storeService: MockedObject<StoreService>;
    let idGeneratorService: MockedObject<IdGeneratorService>;
    let mockSelectChats: MemoizedSelector<
        object,
        readonly IChatDto[],
        DefaultProjectorFn<readonly IChatDto[]>
    >;
    let mockSelectAccounts: MemoizedSelector<
        object,
        readonly IAccountDto[],
        DefaultProjectorFn<readonly IAccountDto[]>
    >;

    const account1: IAccountDto = {
        id: '1',
        firstName: 'John',
        lastName: 'Doe',
        accountTypeId: 1,
        image: new ImageDto({
            id: 'img1',
            fileStorageTypeId: 1,
        }),
    };

    const account2: IAccountDto = {
        id: '2',
        firstName: 'Neil',
        lastName: 'Johnston',
        accountTypeId: 1,
        photoUrl: 'url2',
    };

    const chat1: IChatDto = {
        id: 'chat1',
        accountIds: [account1.id, account2.id],
        accountTypeId: 1,
        chatName: 'Neil Johnston',
        photoUrl: 'url2',
        isIndividual: true,
    };

    beforeEach(async () => {
        storeService = {
            initAccountStorage: vi
                .fn()
                .mockName('StoreService.initAccountStorage'),
            markAllAsRead: vi.fn().mockName('StoreService.markAllAsRead'),
            addChat: vi.fn().mockName('StoreService.addChat'),
        } as MockedObject<StoreService>;
        idGeneratorService = {
            getNextFakeId: vi.fn().mockName('IdGeneratorService.getNextFakeId'),
        } as MockedObject<IdGeneratorService>;
        idGeneratorService.getNextFakeId.mockReturnValue(1);

        await TestBed.configureTestingModule({
            declarations: [
                AccountListComponent,
                AccountListItemStubComponent,
                OrderByPipe,
            ],
            providers: [
                provideMockStore({}),
                { provide: StoreService, useValue: storeService },
                { provide: IdGeneratorService, useValue: idGeneratorService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AccountListComponent);
        component = fixture.componentInstance;
        store = TestBed.inject(Store) as MockStore;
        mockSelectChats = store.overrideSelector(selectChats, null);
        mockSelectAccounts = store.overrideSelector(selectAccounts, null);
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render AccountListItemComponent sorted by firstName, then bby lastName', () => {
        // Arrange
        const account3: IAccountDto = {
            firstName: 'Bob',
            lastName: 'Pettit',
        };
        const account4: IAccountDto = {
            firstName: 'George',
            lastName: 'Gevin',
        };
        mockSelectAccounts.setResult([account1, account2, account3, account4]);

        // Act
        store.refreshState();
        fixture.detectChanges();

        // Assert
        expect(storeService.initAccountStorage).toHaveBeenCalledTimes(1);

        const accountListItems = fixture.debugElement
            .queryAll(By.directive(AccountListItemStubComponent))
            .map((element) => element.componentInstance);

        expect(accountListItems.length).toBe(4);
        expect(accountListItems[0].account).toBe(account3);
        expect(accountListItems[1].account).toBe(account4);
        expect(accountListItems[2].account).toBe(account1);
        expect(accountListItems[3].account).toBe(account2);
    });

    it('should call markAllAsRead when onAccountSelected is called with an existing chat', async () => {
        // Arrange
        mockSelectAccounts.setResult([account1]);
        mockSelectChats.setResult([chat1]);

        // Act
        store.refreshState();
        fixture.detectChanges();
        await component.onAccountSelected(account1);

        // Assert
        expect(storeService.initAccountStorage).toHaveBeenCalledTimes(1);
        expect(storeService.markAllAsRead).toHaveBeenCalledTimes(1);
        expect(storeService.markAllAsRead).toHaveBeenCalledWith(chat1);
        expect(idGeneratorService.getNextFakeId).not.toHaveBeenCalledTimes(1);
        expect(storeService.addChat).not.toHaveBeenCalledTimes(1);
    });

    it('should create a new chat when onAccountSelected is called with a new account', async () => {
        // Arrange
        const newChatId = -1;
        mockSelectAccounts.setResult([account1]);
        mockSelectChats.setResult([]);
        idGeneratorService.getNextFakeId.mockReturnValue(newChatId);

        // Act
        store.refreshState();
        fixture.detectChanges();
        await component.onAccountSelected(account1);

        // Assert
        expect(storeService.initAccountStorage).toHaveBeenCalledTimes(1);
        expect(storeService.markAllAsRead).not.toHaveBeenCalledTimes(1);
        expect(idGeneratorService.getNextFakeId).toHaveBeenCalledTimes(1);
        expect(storeService.addChat).toHaveBeenCalledTimes(1);
    });
});
