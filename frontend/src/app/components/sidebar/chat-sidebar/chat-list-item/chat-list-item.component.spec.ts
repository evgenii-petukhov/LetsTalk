import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ChatListItemComponent } from './chat-list-item.component';
import { IChatDto, ImageDto } from '../../../../api-client/api-client';
import { AvatarStubComponent } from '../../../shared/avatar/avatar.component.stub';
import { UserDetailsStubComponent } from '../../../shared/user-details/user-details.component.stub';
import { UnreadCountStubComponent } from '../unread-count/unread-count.component.stub';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

describe('ChatListItemComponent', () => {
    let component: ChatListItemComponent;
    let fixture: ComponentFixture<ChatListItemComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [
                ChatListItemComponent,
                AvatarStubComponent,
                UserDetailsStubComponent,
                UnreadCountStubComponent,
            ],
            imports: [RouterTestingModule],
        }).compileComponents();

        fixture = TestBed.createComponent(ChatListItemComponent);
        component = fixture.componentInstance;
    });

    it('should create the component', () => {
        expect(component).toBeTruthy();
    });

    it('should render the AvatarComponent, UserDetailsComponent, and UnreadCountComponent with correct values', () => {
        // Arrange
        const chat: IChatDto = {
            id: 'chat-id',
            chatName: 'Chat Name',
            unreadCount: 5,
            photoUrl: 'photo-url',
            image: new ImageDto({
                id: 'image-id',
                fileStorageTypeId: 1,
            }),
        };

        // Act
        component.chat = chat;
        fixture.detectChanges();

        // Assert
        const avatarComponent = fixture.debugElement.query(
            By.directive(AvatarStubComponent),
        ).componentInstance as AvatarStubComponent;
        expect(avatarComponent.urlOptions).toEqual([chat.image, chat.photoUrl]);

        const userDetailsComponent = fixture.debugElement.query(
            By.directive(UserDetailsStubComponent),
        ).componentInstance as UserDetailsStubComponent;
        expect(userDetailsComponent.value).toEqual(`${chat.chatName}`);

        const unreadCountComponent = fixture.debugElement.query(
            By.directive(UnreadCountStubComponent),
        ).componentInstance as UnreadCountStubComponent;
        expect(unreadCountComponent.value.toString()).toEqual(
            `${chat.unreadCount}`,
        );
    });

    it('should not render the UnreadCountComponent if unreadCount is zero', () => {
        // Arrange
        const chat: IChatDto = {
            id: 'chat-id',
            chatName: 'Chat Name',
            unreadCount: 0,
            photoUrl: 'photo-url',
            image: new ImageDto({
                id: 'image-id',
                fileStorageTypeId: 1,
            }),
        };

        // Act
        component.chat = chat;
        fixture.detectChanges();

        // Assert
        const avatarComponent = fixture.debugElement.query(
            By.directive(AvatarStubComponent),
        ).componentInstance as AvatarStubComponent;
        expect(avatarComponent.urlOptions).toEqual([chat.image, chat.photoUrl]);

        const userDetailsComponent = fixture.debugElement.query(
            By.directive(UserDetailsStubComponent),
        ).componentInstance as UserDetailsStubComponent;
        expect(userDetailsComponent.value).toEqual(`${chat.chatName}`);

        const unreadCountComponent = fixture.debugElement.query(
            By.directive(UnreadCountStubComponent),
        );
        expect(unreadCountComponent).toBeNull();
    });
});
