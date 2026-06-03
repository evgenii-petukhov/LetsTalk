import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LogoutButtonComponent } from './logout-button.component';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('LogoutButtonComponent', () => {
    let component: LogoutButtonComponent;
    let fixture: ComponentFixture<LogoutButtonComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [LogoutButtonComponent],
            imports: [FontAwesomeModule],
        }).compileComponents();

        fixture = TestBed.createComponent(LogoutButtonComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should display the fa-right-from-bracket icon', () => {
        // Arrange

        // Act
        fixture.detectChanges();

        // Assert
        const icon = fixture.debugElement.query(By.css('fa-icon'));
        expect(icon).toBeTruthy();
        expect(icon.componentInstance.icon()).toEqual(
            component.faRightFromBracket,
        );
    });

    it('should emit the buttonClick event when the button is clicked', () => {
        // Arrange
        vi.spyOn(component.buttonClick, 'emit');

        // Act
        const button = fixture.debugElement.query(By.css('a')).nativeElement;
        button.click();

        // Assert
        expect(component.buttonClick.emit).toHaveBeenCalled();
    });
});
