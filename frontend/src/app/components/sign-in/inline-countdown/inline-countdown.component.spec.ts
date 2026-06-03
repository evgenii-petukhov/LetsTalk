import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InlineCountdownComponent } from './inline-countdown.component';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

describe('InlineCountdownComponent', () => {
    let component: InlineCountdownComponent;
    let fixture: ComponentFixture<InlineCountdownComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [InlineCountdownComponent],
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(InlineCountdownComponent);
        component = fixture.componentInstance;
        component.startValue = 5;

        // Use Vitest's fake timers
        vi.useFakeTimers();
    });

    afterEach(() => {
        // Clean up timers
        vi.useRealTimers();
    });

    it('should emit expired event when countdown reaches zero', () => {
        // Arrange
        vi.spyOn(component.expired, 'emit');

        // Act
        component.ngOnInit();
        vi.advanceTimersByTime(5000);

        // Assert
        expect(component.value()).toBe(0);
        expect(component.expired.emit).toHaveBeenCalled();

        component.ngOnDestroy();
    });

    it('should decrement value every second', () => {
        // Arrange

        // Act
        component.ngOnInit();
        vi.advanceTimersByTime(1000);

        // Assert
        expect(component.value()).toBe(4);

        vi.advanceTimersByTime(1000);
        expect(component.value()).toBe(3);

        component.ngOnDestroy();
    });

    it('should stop the timer when countdown reaches zero', () => {
        // Arrange
        vi.spyOn(window, 'clearInterval');

        // Act
        component.ngOnInit();
        vi.advanceTimersByTime(5000);

        // Assert
        expect(component.value()).toBe(0);
        expect(window.clearInterval).toHaveBeenCalled();

        component.ngOnDestroy();
    });
});
