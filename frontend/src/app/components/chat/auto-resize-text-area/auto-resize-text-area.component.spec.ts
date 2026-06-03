import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { AutoResizeTextAreaComponent } from './auto-resize-text-area.component';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('AutoResizeTextAreaComponent', () => {
    let component: AutoResizeTextAreaComponent;
    let fixture: ComponentFixture<AutoResizeTextAreaComponent>;
    let textareaElement: HTMLTextAreaElement;
    let textareaDebugElement: DebugElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [AutoResizeTextAreaComponent],
            imports: [FormsModule],
        }).compileComponents();

        fixture = TestBed.createComponent(AutoResizeTextAreaComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();

        textareaDebugElement = fixture.debugElement.query(By.css('textarea'));
        textareaElement = textareaDebugElement.nativeElement;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    describe('Component Properties', () => {
        it('should have default text as empty string', () => {
            expect(component.text).toBe('');
        });

        it('should have textChange EventEmitter', () => {
            expect(component.textChange).toBeDefined();
            expect(component.textChange.emit).toBeDefined();
        });

        it('should have submitted EventEmitter', () => {
            expect(component.submitted).toBeDefined();
            expect(component.submitted.emit).toBeDefined();
        });

        it('should have textareaRef ViewChild', () => {
            expect(component.textareaRef).toBeDefined();
            expect(component.textareaRef.nativeElement).toBe(textareaElement);
        });
    });

    describe('Template Rendering', () => {
        it('should render textarea element', () => {
            expect(textareaElement).toBeTruthy();
        });

        it('should have correct textarea attributes', () => {
            expect(textareaElement.name).toBe('message');
            expect(textareaElement.rows).toBe(1);
        });

        it('should bind text property to textarea value', async () => {
            component.text = 'Test message';
            fixture.detectChanges();
            await fixture.whenStable();

            expect(textareaElement.value).toBe('Test message');
        });

        it('should update component text when textarea value changes', async () => {
            textareaElement.value = 'New text';
            textareaElement.dispatchEvent(new Event('input'));
            fixture.detectChanges();
            await fixture.whenStable();

            expect(component.text).toBe('New text');
        });
    });

    describe('focus() method', () => {
        it('should set textarea height to auto and focus the element', () => {
            vi.spyOn(textareaElement, 'focus');

            component.focus();

            expect(textareaElement.style.height).toBe('auto');
            expect(textareaElement.focus).toHaveBeenCalled();
        });
    });

    describe('onTextChanged() method', () => {
        beforeEach(() => {
            vi.spyOn(component.textChange, 'emit');
        });

        it('should reset height to auto and then set to scrollHeight', () => {
            // Mock scrollHeight
            Object.defineProperty(textareaElement, 'scrollHeight', {
                value: 80,
                writable: true,
            });

            // Set initial height to something different
            textareaElement.style.height = '100px';

            component.onTextChanged();

            // After onTextChanged, height should be set to scrollHeight
            expect(textareaElement.style.height).toBe('80px');
        });

        it('should set height to scrollHeight', () => {
            Object.defineProperty(textareaElement, 'scrollHeight', {
                value: 100,
                writable: true,
            });

            component.onTextChanged();

            expect(textareaElement.style.height).toBe('100px');
        });

        it('should emit textChange event with current text', () => {
            component.text = 'Test text';

            component.onTextChanged();

            expect(component.textChange.emit).toHaveBeenCalledWith('Test text');
        });

        it('should be called when ngModelChange is triggered', () => {
            vi.spyOn(component, 'onTextChanged');

            textareaElement.value = 'Changed text';
            textareaElement.dispatchEvent(new Event('input'));
            fixture.detectChanges();

            expect(component.onTextChanged).toHaveBeenCalled();
        });
    });

    describe('submit() method', () => {
        it('should emit submitted event with current text', () => {
            vi.spyOn(component.submitted, 'emit');
            component.text = 'Submit this text';

            component.submit();

            expect(component.submitted.emit).toHaveBeenCalledWith(
                'Submit this text',
            );
        });

        it('should be called when Ctrl+Enter is pressed', () => {
            vi.spyOn(component, 'submit');

            const keydownEvent = new KeyboardEvent('keydown', {
                key: 'Enter',
                ctrlKey: true,
            });

            textareaElement.dispatchEvent(keydownEvent);
            fixture.detectChanges();

            expect(component.submit).toHaveBeenCalled();
        });
    });

    describe('Input/Output Integration', () => {
        it('should update textarea when text input changes', async () => {
            component.text = 'Initial text';
            fixture.detectChanges();
            await fixture.whenStable();

            expect(textareaElement.value).toBe('Initial text');

            component.text = 'Updated text';
            fixture.detectChanges();
            await fixture.whenStable();

            expect(textareaElement.value).toBe('Updated text');
        });

        it('should emit textChange when text is modified', () => {
            let emittedText: string;
            component.textChange.subscribe((text: string) => {
                emittedText = text;
            });

            component.text = 'Emitted text';
            component.onTextChanged();

            expect(emittedText).toBe('Emitted text');
        });

        it('should emit submitted when submit is called', () => {
            let submittedText: string;
            component.submitted.subscribe((text: string) => {
                submittedText = text;
            });

            component.text = 'Submitted text';
            component.submit();

            expect(submittedText).toBe('Submitted text');
        });
    });

    describe('Auto-resize functionality', () => {
        it('should adjust height based on content', () => {
            // Mock scrollHeight to simulate content height
            Object.defineProperty(textareaElement, 'scrollHeight', {
                value: 150,
                writable: true,
            });

            component.onTextChanged();

            expect(textareaElement.style.height).toBe('150px');
        });

        it('should reset height before calculating new height', () => {
            textareaElement.style.height = '200px';

            component.onTextChanged();

            // Height should be reset to auto first, then set to scrollHeight
            expect(textareaElement.style.height).not.toBe('200px');
        });
    });

    describe('Edge Cases', () => {
        it('should handle empty text', () => {
            vi.spyOn(component.textChange, 'emit');
            component.text = '';

            component.onTextChanged();

            expect(component.textChange.emit).toHaveBeenCalledWith('');
        });

        it('should handle submit with empty text', () => {
            vi.spyOn(component.submitted, 'emit');
            component.text = '';

            component.submit();

            expect(component.submitted.emit).toHaveBeenCalledWith('');
        });

        it('should handle very long text', () => {
            const longText = 'a'.repeat(1000);
            vi.spyOn(component.textChange, 'emit');
            component.text = longText;

            component.onTextChanged();

            expect(component.textChange.emit).toHaveBeenCalledWith(longText);
        });
    });
});
