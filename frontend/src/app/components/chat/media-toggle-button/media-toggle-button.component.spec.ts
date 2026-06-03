/* eslint-disable @typescript-eslint/no-explicit-any */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { EventEmitter } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
    faMicrophone,
    faMicrophoneSlash,
    faVideo,
    faVideoSlash,
} from '@fortawesome/free-solid-svg-icons';

import { MediaToggleButtonComponent } from './media-toggle-button.component';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('MediaToggleButtonComponent', () => {
    let component: MediaToggleButtonComponent;
    let fixture: ComponentFixture<MediaToggleButtonComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [MediaToggleButtonComponent],
            imports: [FontAwesomeModule],
        }).compileComponents();

        fixture = TestBed.createComponent(MediaToggleButtonComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    describe('Default values', () => {
        it('should have default mode as audio', () => {
            expect(component.mode).toBe('audio');
        });

        it('should have default isMuted as false', () => {
            expect(component.isMuted).toBe(false);
        });
    });

    describe('Icon properties', () => {
        it('should have all required FontAwesome icons', () => {
            expect(component.faMicrophone).toBe(faMicrophone);
            expect(component.faMicrophoneSlash).toBe(faMicrophoneSlash);
            expect(component.faVideo).toBe(faVideo);
            expect(component.faVideoSlash).toBe(faVideoSlash);
        });
    });

    describe('activeIcon getter', () => {
        describe('when mode is audio', () => {
            beforeEach(() => {
                component.mode = 'audio';
            });

            it('should return faMicrophone when not muted', () => {
                component.isMuted = false;
                expect(component.activeIcon).toBe(faMicrophone);
            });

            it('should return faMicrophoneSlash when muted', () => {
                component.isMuted = true;
                expect(component.activeIcon).toBe(faMicrophoneSlash);
            });
        });

        describe('when mode is video', () => {
            beforeEach(() => {
                component.mode = 'video';
            });

            it('should return faVideo when not muted', () => {
                component.isMuted = false;
                expect(component.activeIcon).toBe(faVideo);
            });

            it('should return faVideoSlash when muted', () => {
                component.isMuted = true;
                expect(component.activeIcon).toBe(faVideoSlash);
            });
        });
    });

    describe('Button click functionality', () => {
        it('should emit buttonClick event when button is clicked', () => {
            vi.spyOn(component.buttonClick, 'emit');

            const button = fixture.debugElement.query(By.css('button'));
            button.nativeElement.click();

            expect(component.buttonClick.emit).toHaveBeenCalled();
        });

        it('should call onButtonClicked when button is clicked', () => {
            vi.spyOn(component, 'onButtonClicked');

            const button = fixture.debugElement.query(By.css('button'));
            button.nativeElement.click();

            expect(component.onButtonClicked).toHaveBeenCalled();
        });

        it('should emit buttonClick event when onButtonClicked is called directly', () => {
            vi.spyOn(component.buttonClick, 'emit');
            const mockEvent = new MouseEvent('click');

            component.onButtonClicked(mockEvent);

            expect(component.buttonClick.emit).toHaveBeenCalledWith(mockEvent);
        });
    });

    describe('Template rendering', () => {
        it('should render a button element', () => {
            const button = fixture.debugElement.query(By.css('button'));
            expect(button).toBeTruthy();
        });

        it('should render fa-icon component', () => {
            const faIcon = fixture.debugElement.query(By.css('fa-icon'));
            expect(faIcon).toBeTruthy();
        });

        it('should pass activeIcon to fa-icon component', () => {
            component.mode = 'audio';
            component.isMuted = false;
            fixture.detectChanges();

            expect(component.activeIcon).toEqual(faMicrophone);
        });

        it('should update icon when mode changes', () => {
            // Start with audio mode
            component.mode = 'audio';
            component.isMuted = false;
            fixture.detectChanges();

            expect(component.activeIcon).toEqual(faMicrophone);

            // Change to video mode
            component.mode = 'video';
            fixture.detectChanges();

            expect(component.activeIcon).toEqual(faVideo);
        });

        it('should update icon when isMuted changes', () => {
            // Start unmuted
            component.mode = 'audio';
            component.isMuted = false;
            fixture.detectChanges();

            expect(component.activeIcon).toEqual(faMicrophone);

            // Change to muted
            component.isMuted = true;
            fixture.detectChanges();

            expect(component.activeIcon).toEqual(faMicrophoneSlash);
        });
    });

    describe('Input property changes', () => {
        it('should handle mode input changes', () => {
            component.mode = 'video';
            component.isMuted = false;
            expect(component.activeIcon).toBe(faVideo);

            component.mode = 'audio';
            expect(component.activeIcon).toBe(faMicrophone);
        });

        it('should handle isMuted input changes', () => {
            component.mode = 'audio';
            component.isMuted = true;
            expect(component.activeIcon).toBe(faMicrophoneSlash);

            component.isMuted = false;
            expect(component.activeIcon).toBe(faMicrophone);
        });
    });

    describe('EventEmitter', () => {
        it('should have buttonClick as EventEmitter', () => {
            expect(component.buttonClick).toBeInstanceOf(EventEmitter);
        });

        it('should emit void type', () => {
            let emittedValue: any;
            const mockEvent = new MouseEvent('click');
            component.buttonClick.subscribe((value) => (emittedValue = value));

            component.onButtonClicked(mockEvent);

            expect(emittedValue).toBe(mockEvent);
        });
    });
});
