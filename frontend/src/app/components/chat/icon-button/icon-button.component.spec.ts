import { ComponentFixture, TestBed } from '@angular/core/testing';
import { IconButtonComponent } from './icon-button.component';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { By } from '@angular/platform-browser';
import {
    faCameraRotate,
    faDownLeftAndUpRightToCenter,
} from '@fortawesome/free-solid-svg-icons';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('IconButtonComponent', () => {
    let component: IconButtonComponent;
    let fixture: ComponentFixture<IconButtonComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [IconButtonComponent],
            imports: [FontAwesomeModule],
        }).compileComponents();

        fixture = TestBed.createComponent(IconButtonComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should default mode to "minimize"', () => {
        expect(component.mode).toBe('minimize');
    });

    describe('activeIcon', () => {
        it('should return faDownLeftAndUpRightToCenter when mode is "minimize"', () => {
            component.mode = 'minimize';
            expect(component.activeIcon).toEqual(faDownLeftAndUpRightToCenter);
        });

        it('should return faCameraRotate when mode is "switch-camera"', () => {
            component.mode = 'switch-camera';
            expect(component.activeIcon).toEqual(faCameraRotate);
        });
    });

    describe('onButtonClicked', () => {
        it('should emit buttonClick with the mouse event', () => {
            vi.spyOn(component.buttonClick, 'emit');
            const mockEvent = new MouseEvent('click');

            component.onButtonClicked(mockEvent);

            expect(component.buttonClick.emit).toHaveBeenCalledWith(mockEvent);
        });

        it('should emit buttonClick on each call', () => {
            vi.spyOn(component.buttonClick, 'emit');
            const mockEvent = new MouseEvent('click');

            component.onButtonClicked(mockEvent);
            component.onButtonClicked(mockEvent);

            expect(component.buttonClick.emit).toHaveBeenCalledTimes(2);
        });
    });

    describe('template', () => {
        it('should render a button element', () => {
            const button = fixture.debugElement.query(By.css('button'));
            expect(button).toBeTruthy();
        });

        it('should render an fa-icon inside the button', () => {
            const faIcon = fixture.debugElement.query(By.css('button fa-icon'));
            expect(faIcon).toBeTruthy();
        });

        it('should emit buttonClick when the button is clicked', () => {
            vi.spyOn(component.buttonClick, 'emit');
            const button = fixture.debugElement.query(By.css('button'));

            button.triggerEventHandler('click', new MouseEvent('click'));

            expect(component.buttonClick.emit).toHaveBeenCalled();
        });

        it('should call onButtonClicked when the button is clicked', () => {
            vi.spyOn(component, 'onButtonClicked');
            const button = fixture.debugElement.query(By.css('button'));
            const mockEvent = new MouseEvent('click');

            button.triggerEventHandler('click', mockEvent);

            expect(component.onButtonClicked).toHaveBeenCalledWith(mockEvent);
        });
    });
});
