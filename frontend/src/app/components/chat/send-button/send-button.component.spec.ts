import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SendButtonComponent } from './send-button.component';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe(SendButtonComponent.name, () => {
    let component: SendButtonComponent;
    let fixture: ComponentFixture<SendButtonComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [SendButtonComponent],
            imports: [FontAwesomeModule],
        }).compileComponents();

        fixture = TestBed.createComponent(SendButtonComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should display the faPaperPlane icon', () => {
        // Arrange
        const iconElement = fixture.debugElement.query(By.css('fa-icon'));

        // Act
        fixture.detectChanges();

        // Assert
        expect(iconElement).toBeTruthy();
        expect(iconElement.componentInstance.icon()).toEqual(
            component.faPaperPlane,
        );
    });

    it('should emit buttonClick when the button is clicked', () => {
        // Arrange
        vi.spyOn(component.buttonClick, 'emit');

        // Act
        const buttonElement = fixture.debugElement.query(
            By.css('button'),
        ).nativeElement;
        buttonElement.click();

        // Assert
        expect(component.buttonClick.emit).toHaveBeenCalled();
    });

    it('should disable the button when the disabled input is true', () => {
        // Arrange
        component.disabled = true;

        // Act
        fixture.detectChanges();

        // Assert
        const buttonElement = fixture.debugElement.query(
            By.css('button'),
        ).nativeElement;
        expect(buttonElement.disabled).toBe(true);
    });

    it('should enable the button when the disabled input is false', () => {
        // Arrange
        component.disabled = false;

        // Act
        fixture.detectChanges();

        // Assert
        const buttonElement = fixture.debugElement.query(
            By.css('button'),
        ).nativeElement;
        expect(buttonElement.disabled).toBe(false);
    });
});
