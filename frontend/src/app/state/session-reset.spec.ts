import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Store, StoreModule } from '@ngrx/store';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../services/api.service';
import { SessionService } from '../services/session.service';
import { storeConfig, storeMetaReducers } from './store-config';
import { loggedInUserActions } from './logged-in-user/logged-in-user.actions';
import { selectedChatInfoActions } from './selected-chat/selected-chat-info.actions';
import { selectLoggedInUser } from './logged-in-user/logged-in-user.selectors';

// The real store configuration with the real SessionService: ending the session must
// leave nothing of the previous account behind (the page is not reloaded any more)
describe('session end resets the store', () => {
    let store: Store;
    let sessionService: SessionService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [
                StoreModule.forRoot(storeConfig, {
                    metaReducers: storeMetaReducers,
                }),
            ],
            providers: [
                SessionService,
                {
                    provide: ApiService,
                    useValue: { getProfile: vi.fn(), logout: vi.fn() },
                },
            ],
        });
        store = TestBed.inject(Store);
        sessionService = TestBed.inject(SessionService);
    });

    it('should return every slice to its initial state when the session is lost', async () => {
        const initialState = await firstValueFrom(store);
        store.dispatch(
            loggedInUserActions.init({
                account: { id: '1', firstName: 'Ann', lastName: 'Lee' },
            }),
        );
        store.dispatch(selectedChatInfoActions.init({ chatId: 'chat-1' }));
        expect(await firstValueFrom(store.select(selectLoggedInUser))).toBeTruthy();

        sessionService.onSessionLost();

        expect(await firstValueFrom(store)).toEqual(initialState);
        expect(await firstValueFrom(store.select(selectLoggedInUser))).toBeNull();
    });
});
