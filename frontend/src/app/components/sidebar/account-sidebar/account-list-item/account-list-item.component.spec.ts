import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AccountListItemComponent } from './account-list-item.component';
import { IAccountDto, ImageDto } from '../../../../api-client/api-client';
import { By } from '@angular/platform-browser';
import { AvatarStubComponent } from '../../../shared/avatar/avatar.component.stub';
import { UserDetailsStubComponent } from '../../../shared/user-details/user-details.component.stub';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('AccountListItemComponent', () => {
    let component: AccountListItemComponent;
    let fixture: ComponentFixture<AccountListItemComponent>;

    const account = {
        id: '1',
        firstName: 'John',
        lastName: 'Doe',
        image: new ImageDto({
            id: 'image1',
            fileStorageTypeId: 1,
        }),
        photoUrl: 'https://example.com/photo.jpg',
    } as IAccountDto;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [
                AccountListItemComponent,
                AvatarStubComponent,
                UserDetailsStubComponent,
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AccountListItemComponent);
        component = fixture.componentInstance;
    });

    it('should render the AvatarComponent and UserDetailsComponent with correct values', () => {
        // Arrange

        // Act
        component.account = account;
        fixture.detectChanges();

        // Assert
        const avatarComponent = fixture.debugElement.query(
            By.directive(AvatarStubComponent),
        ).componentInstance as AvatarStubComponent;
        expect(avatarComponent.urlOptions).toEqual([
            account.image,
            account.photoUrl,
        ]);

        const userDetailsComponent = fixture.debugElement.query(
            By.directive(UserDetailsStubComponent),
        ).componentInstance as UserDetailsStubComponent;
        expect(userDetailsComponent.value).toEqual(
            `${account.firstName} ${account.lastName}`,
        );
    });

    it('should emit an event when onAccountSelected is called', () => {
        // Arrange
        vi.spyOn(component.accountSelected, 'emit');

        // Act
        component.account = account;
        fixture.detectChanges();

        component.onAccountSelected();

        // Assert
        expect(component.accountSelected.emit).toHaveBeenCalledTimes(1);

        // Assert
        expect(component.accountSelected.emit).toHaveBeenCalledWith(account);
    });

    it('should emit an event when link element is clicked', () => {
        // Arrange
        vi.spyOn(component.accountSelected, 'emit');

        // Act
        component.account = account;
        fixture.detectChanges();

        fixture.debugElement
            .query(By.css('a'))
            .triggerEventHandler('click', null);

        // Assert
        expect(component.accountSelected.emit).toHaveBeenCalledTimes(1);

        // Assert
        expect(component.accountSelected.emit).toHaveBeenCalledWith(account);
    });
});
