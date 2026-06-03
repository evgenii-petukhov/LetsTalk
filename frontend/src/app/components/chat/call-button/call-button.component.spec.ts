import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CallButtonComponent } from './call-button.component';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('CallButtonComponent', () => {
    let component: CallButtonComponent;
    let fixture: ComponentFixture<CallButtonComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [CallButtonComponent],
            imports: [FontAwesomeModule],
        }).compileComponents();

        fixture = TestBed.createComponent(CallButtonComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have default mode as "start-call"', () => {
        expect(component.mode).toBe('start-call');
    });

    it('should have faPhone icon defined', () => {
        expect(component.faPhone).toBeDefined();
    });

    describe('Input Properties', () => {
        it('should accept "start-call" mode', () => {
            // Arrange & Act
            component.mode = 'start-call';
            fixture.detectChanges();

            // Assert
            expect(component.mode).toBe('start-call');
        });

        it('should accept "end-call" mode', () => {
            // Arrange & Act
            component.mode = 'end-call';
            fixture.detectChanges();

            // Assert
            expect(component.mode).toBe('end-call');
        });
    });

    describe('Output Events', () => {
        it('should emit buttonClick event when onButtonClicked is called', () => {
            // Arrange
            vi.spyOn(component.buttonClick, 'emit');
            const mockEvent = new MouseEvent('click');

            // Act
            component.onButtonClicked(mockEvent);

            // Assert
            expect(component.buttonClick.emit).toHaveBeenCalled();
        });

        it('should emit buttonClick event with the mouse event', () => {
            // Arrange
            vi.spyOn(component.buttonClick, 'emit');
            const mockEvent = new MouseEvent('click');

            // Act
            component.onButtonClicked(mockEvent);

            // Assert
            expect(component.buttonClick.emit).toHaveBeenCalledWith(mockEvent);
        });

        it('should emit buttonClick event when button is clicked in template', () => {
            // Arrange
            vi.spyOn(component.buttonClick, 'emit');
            fixture.detectChanges();

            // Find the button element (assuming it has a button tag or specific selector)
            const buttonElement = fixture.debugElement.query(By.css('button'));

            // Act
            if (buttonElement) {
                buttonElement.nativeElement.click();
            } else {
                // If no button element, trigger click on the component root
                const componentElement = fixture.debugElement.nativeElement;
                componentElement.click();
            }

            // Assert
            expect(component.buttonClick.emit).toHaveBeenCalled();
        });
    });

    describe('Template Integration', () => {
        it('should render with start-call mode', () => {
            // Arrange
            component.mode = 'start-call';

            // Act
            fixture.detectChanges();

            // Assert
            expect(fixture.debugElement.nativeElement).toBeTruthy();
            // Additional template-specific assertions can be added here
        });

        it('should render with end-call mode', () => {
            // Arrange
            component.mode = 'end-call';

            // Act
            fixture.detectChanges();

            // Assert
            expect(fixture.debugElement.nativeElement).toBeTruthy();
            // Additional template-specific assertions can be added here
        });

        it('should display FontAwesome phone icon', () => {
            // Arrange & Act
            fixture.detectChanges();

            // Assert
            const faIcon = fixture.debugElement.query(By.css('fa-icon'));
            expect(faIcon).toBeTruthy();
        });
    });

    describe('Event Handling', () => {
        it('should call onButtonClicked when button is clicked', () => {
            // Arrange
            vi.spyOn(component, 'onButtonClicked');
            fixture.detectChanges();

            // Act
            const buttonElement = fixture.debugElement.query(By.css('button'));
            buttonElement.triggerEventHandler('click', new MouseEvent('click'));

            // Assert
            expect(component.onButtonClicked).toHaveBeenCalled();
        });
    });

    describe('Integration with Parent Component', () => {
        it('should work correctly when used with input binding', () => {
            // Arrange
            const testMode = 'end-call';

            // Act
            component.mode = testMode;
            fixture.detectChanges();

            // Assert
            expect(component.mode).toBe(testMode);
        });

        it('should work correctly when used with output binding', async () => {
            // Arrange
            const mockEvent = new MouseEvent('click');
            component.buttonClick.subscribe((event) => {
                // Assert
                expect(event).toBe(mockEvent);
            });

            // Act
            component.onButtonClicked(mockEvent);
        });
    });

    describe('Edge Cases', () => {
        it('should handle rapid successive clicks', () => {
            // Arrange
            vi.spyOn(component.buttonClick, 'emit');
            const mockEvent = new MouseEvent('click');

            // Act
            component.onButtonClicked(mockEvent);
            component.onButtonClicked(mockEvent);
            component.onButtonClicked(mockEvent);

            // Assert
            expect(component.buttonClick.emit).toHaveBeenCalledTimes(3);
        });

        it('should maintain functionality after multiple interactions', () => {
            // Arrange
            const originalMode = component.mode;
            vi.spyOn(component.buttonClick, 'emit');
            const mockEvent = new MouseEvent('click');

            // Act
            component.onButtonClicked(mockEvent);

            // Assert
            expect(component.mode).toBe(originalMode);
            expect(component.buttonClick.emit).toHaveBeenCalled();
        });
    });
});
