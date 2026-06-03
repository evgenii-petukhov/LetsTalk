import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BackButtonComponent } from './back-button.component';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('BackButtonComponent', () => {
    let component: BackButtonComponent;
    let fixture: ComponentFixture<BackButtonComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [BackButtonComponent],
            imports: [FontAwesomeModule],
        }).compileComponents();

        fixture = TestBed.createComponent(BackButtonComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should display the fa-chevron-left icon', () => {
        // Arrange

        // Act
        fixture.detectChanges();

        // Assert
        const icon = fixture.debugElement.query(By.css('fa-icon'));
        expect(icon).toBeTruthy();
        expect(icon.componentInstance.icon()).toEqual(component.faChevronLeft);
    });

    it('should emit the buttonClick event when the button is clicked', () => {
        // Arrange
        vi.spyOn(component.buttonClick, 'emit');

        // Act
        const button = fixture.debugElement.query(
            By.css('button'),
        ).nativeElement;
        button.click();

        // Assert
        expect(component.buttonClick.emit).toHaveBeenCalled();
    });
});
