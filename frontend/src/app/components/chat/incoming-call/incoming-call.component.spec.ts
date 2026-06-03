import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Store } from '@ngrx/store';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { By } from '@angular/platform-browser';
import { IncomingCallComponent } from './incoming-call.component';
import { StoreService } from '../../../services/store.service';
import { AvatarStubComponent } from '../../shared/avatar/avatar.component.stub';
import { CallButtonStubComponent } from '../call-button/call-button.component.stub';
import { selectCaller } from '../../../state/video-call/video-call.selectors';
import { IChatDto, ImageDto } from '../../../api-client/api-client';
import { DefaultProjectorFn, MemoizedSelector } from '@ngrx/store';

describe('IncomingCallComponent', () => {
    let fixture: ComponentFixture<IncomingCallComponent>;
    let store: MockStore;
    let storeService: MockedObject<StoreService>;
    let mockSelectCaller: MemoizedSelector<
        object,
        IChatDto,
        DefaultProjectorFn<IChatDto>
    >;

    const mockCaller = {
        id: 'chat1',
        chatName: 'Alice',
        image: new ImageDto({ id: 'img1', fileStorageTypeId: 1 }),
        photoUrl: 'https://example.com/photo.jpg',
    } as IChatDto;

    beforeEach(async () => {
        storeService = {
            acceptIncomingCall: vi
                .fn()
                .mockName('StoreService.acceptIncomingCall'),
            resetCall: vi.fn().mockName('StoreService.resetCall'),
        } as MockedObject<StoreService>;

        await TestBed.configureTestingModule({
            declarations: [
                IncomingCallComponent,
                AvatarStubComponent,
                CallButtonStubComponent,
            ],
            providers: [
                provideMockStore({}),
                { provide: StoreService, useValue: storeService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(IncomingCallComponent);
        store = TestBed.inject(Store) as MockStore;
        mockSelectCaller = store.overrideSelector(selectCaller, null);
    });

    it('should display caller name when caller is set', () => {
        mockSelectCaller.setResult(mockCaller);
        store.refreshState();
        fixture.detectChanges();

        const nameEl =
            fixture.nativeElement.querySelector('.caller-info__name');
        expect(nameEl.textContent).toContain(mockCaller.chatName);
    });

    it('should pass correct urlOptions to app-avatar', () => {
        mockSelectCaller.setResult(mockCaller);
        store.refreshState();
        fixture.detectChanges();

        const avatar = fixture.debugElement.query(
            By.directive(AvatarStubComponent),
        );
        expect(avatar.componentInstance.urlOptions).toEqual([
            mockCaller.image,
            mockCaller.photoUrl,
        ]);
    });

    it('should have null urlOptions when caller is null', () => {
        mockSelectCaller.setResult(null);
        store.refreshState();
        fixture.detectChanges();

        const component = fixture.componentInstance;
        expect(component.urlOptions()).toBeNull();
    });

    it('should have undefined callerName when caller is null', () => {
        mockSelectCaller.setResult(null);
        store.refreshState();
        fixture.detectChanges();

        expect(fixture.componentInstance.callerName()).toBeUndefined();
    });

    it('should call acceptIncomingCall when accept button is clicked', () => {
        mockSelectCaller.setResult(mockCaller);
        store.refreshState();
        fixture.detectChanges();

        const acceptBtn = fixture.debugElement.query(
            By.css('app-call-button.accept-call'),
        );
        acceptBtn.componentInstance.buttonClick.emit();

        expect(storeService.acceptIncomingCall).toHaveBeenCalledOnce();
    });

    it('should call resetCall when decline button is clicked', () => {
        mockSelectCaller.setResult(mockCaller);
        store.refreshState();
        fixture.detectChanges();

        const buttons = fixture.debugElement.queryAll(
            By.directive(CallButtonStubComponent),
        );
        const declineBtn = buttons[1];
        declineBtn.componentInstance.buttonClick.emit();

        expect(storeService.resetCall).toHaveBeenCalledOnce();
    });

    it('should render accept button with mode accept-call', () => {
        fixture.detectChanges();

        const acceptBtn = fixture.debugElement.query(
            By.css('app-call-button.accept-call'),
        );
        expect(acceptBtn.componentInstance.mode).toBe('accept-call');
    });

    it('should render decline button with mode end-call', () => {
        fixture.detectChanges();

        const buttons = fixture.debugElement.queryAll(
            By.directive(CallButtonStubComponent),
        );
        expect(buttons[1].componentInstance.mode).toBe('end-call');
    });
});
