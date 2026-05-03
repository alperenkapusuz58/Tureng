const header = document.querySelector('header');
const dropdowns = document.querySelectorAll('[data-dropdown]');
let hoverTimeout;

dropdowns.forEach(dropdown => {
    const originalDisplay = dropdown.style.display;
    const originalVisibility = dropdown.style.visibility;
    const originalOpacity = dropdown.style.opacity;

    dropdown.style.display = 'block';
    dropdown.style.visibility = 'hidden';
    dropdown.style.opacity = '0';
    dropdown.style.position = 'absolute';

    const width = dropdown.offsetWidth;

    dropdown.style.display = originalDisplay;
    dropdown.style.visibility = originalVisibility;
    dropdown.style.opacity = originalOpacity;

    if (dropdown.hasAttribute('data-lang-dropdown')) {
        dropdown.style.minWidth = (width + 10) + 'px';
    }
});

const dropdownLabels = document.querySelectorAll('[data-dropdown-trigger]');

function closeAllDropdowns(exceptDropdown = null) {
    document.querySelectorAll('[data-dropdown]').forEach(d => {
        if (d !== exceptDropdown) {
            d.classList.remove('opacity-100', 'visible', 'pointer-events-auto', 'translate-y-0', 'scale-100');
            d.classList.add('opacity-0', 'invisible', 'pointer-events-none', 'translate-y-[-10px]', 'scale-[0.95]');
        }
    });
    document.querySelectorAll('[data-dropdown-trigger]').forEach(l => {
        if (!exceptDropdown || !exceptDropdown.previousElementSibling || l !== exceptDropdown.previousElementSibling) {
            l.classList.remove('text-charcoal');
            l.classList.add('text-orange');
        }
    });
}

dropdownLabels.forEach(label => {
    const dropdown = label.nextElementSibling;
    
    label.addEventListener('click', function (event) {
        event.preventDefault();
        event.stopPropagation();

        const isOpen = dropdown.classList.contains('opacity-100');
        closeAllDropdowns();

        if (!isOpen) {
            dropdown.classList.remove('opacity-0', 'invisible', 'pointer-events-none', 'translate-y-[-10px]', 'scale-[0.95]');
            dropdown.classList.add('opacity-100', 'visible', 'pointer-events-auto', 'translate-y-0', 'scale-100');
            dropdown.focus();
        }
    });

    const dropdownContainer = label.parentElement;
    
    dropdownContainer.addEventListener('mouseenter', () => {
        clearTimeout(hoverTimeout);
        closeAllDropdowns(dropdown);
        dropdown.classList.remove('opacity-0', 'invisible', 'pointer-events-none', 'translate-y-[-10px]', 'scale-[0.95]');
        dropdown.classList.add('opacity-100', 'visible', 'pointer-events-auto', 'translate-y-0', 'scale-100');
    });

    dropdownContainer.addEventListener('mouseleave', () => {
        hoverTimeout = setTimeout(() => {
            dropdown.classList.remove('opacity-100', 'visible', 'pointer-events-auto', 'translate-y-0', 'scale-100');
            dropdown.classList.add('opacity-0', 'invisible', 'pointer-events-none', 'translate-y-[-10px]', 'scale-[0.95]');
        }, 200);
    });
});

document.querySelectorAll('[data-dropdown-item]').forEach(item => {
    item.addEventListener('click', function (event) {
        if (this.closest('[data-lang-dropdown]')) {
            const siblings = Array.from(this.parentNode.children);
            siblings.forEach(sibling => {
                sibling.classList.remove('text-charcoal');
                sibling.classList.add('text-gray');
            });
            this.classList.remove('text-gray');
            this.classList.add('text-charcoal');

            setTimeout(() => {
                const dropdown = this.closest('[data-dropdown]');
                dropdown.classList.remove('opacity-100', 'visible', 'pointer-events-auto', 'translate-y-0', 'scale-100');
                dropdown.classList.add('opacity-0', 'invisible', 'pointer-events-none', 'translate-y-[-10px]', 'scale-[0.95]');
                const label = this.closest('[data-dropdown-container]').querySelector('[data-dropdown-trigger]');
                if (label) {
                    label.classList.remove('text-charcoal');
                    label.classList.add('text-orange');
                }
            }, 100);
        }
    });
});

document.addEventListener('click', function (event) {
    const isDropdown = event.target.closest('[data-dropdown-container]');
    if (!isDropdown) {
        closeAllDropdowns();
    }
});

document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        closeAllDropdowns();
    }
});

const scrollThreshold = 80;

function handleScroll() {
    if (!header) return;
    const shadowBase = 'shadow-[0_4px_12px_rgba(0,0,0,0.08)]';
    const shadowScrolled = 'shadow-[0_4px_16px_rgba(0,0,0,0.08)]';
    if (window.scrollY > scrollThreshold) {
        header.classList.remove(shadowBase);
        header.classList.add(shadowScrolled);
    } else {
        header.classList.remove(shadowScrolled);
        header.classList.add(shadowBase);
    }
}

handleScroll();

window.addEventListener('scroll', handleScroll);

// Sidemenu functionality
const sideMenu = document.querySelector('.Sidemenu');
const overlay = document.querySelector('.Sidemenu-Overlay');
const closeBtn = document.querySelector('[data-side-menu-close]');
const burger = document.querySelector('[data-burger]');

function openMenu() {
    sideMenu.classList.remove('translate-x-full');
    sideMenu.classList.add('translate-x-0');
    overlay.classList.remove('opacity-0', 'pointer-events-none');
    overlay.classList.add('opacity-100', 'pointer-events-auto');
    document.body.style.overflow = 'hidden';
}

function closeMenu() {
    sideMenu.classList.remove('translate-x-0');
    sideMenu.classList.add('translate-x-full');
    overlay.classList.remove('opacity-100', 'pointer-events-auto');
    overlay.classList.add('opacity-0', 'pointer-events-none');
    document.body.style.overflow = '';
}

if (burger) burger.addEventListener('click', openMenu);
if (closeBtn) closeBtn.addEventListener('click', closeMenu);
if (overlay) overlay.addEventListener('click', closeMenu);
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeMenu();
});

// Accordion functionality
const radioInputs = document.querySelectorAll('[data-accordion-input]');
const menuTitles = document.querySelectorAll('[data-accordion-title]');
const collapseItems = document.querySelectorAll('[data-accordion-content]');
const menuLinks = document.querySelectorAll('[data-menu-link]');
const submenuItems = document.querySelectorAll('[data-submenu-item]');

submenuItems.forEach(item => {
    item.addEventListener('click', function (e) {
        e.stopPropagation();
        submenuItems.forEach(otherItem => {
            otherItem.classList.remove('text-[#21484B]');
            otherItem.classList.add('text-[#7A7A7A]');
        });
        this.classList.remove('text-[#7A7A7A]');
        this.classList.add('text-[#21484B]');
    });
});

menuLinks.forEach(link => {
    link.addEventListener('click', function (e) {
        menuLinks.forEach(otherLink => {
            otherLink.classList.remove('text-[#21484B]');
            otherLink.classList.add('text-[#7A7A7A]');
        });
        this.classList.remove('text-[#7A7A7A]');
        this.classList.add('text-[#21484B]');
        menuTitles.forEach(title => {
            title.classList.remove('text-[#21484B]');
            title.classList.add('text-[#7A7A7A]');
        });
        collapseItems.forEach(item => {
            item.classList.remove('flex');
            item.classList.add('hidden');
        });
        radioInputs.forEach(radio => {
            radio.checked = false;
        });
    });
});

radioInputs.forEach((radio, index) => {
    radio.addEventListener('change', function () {
        menuTitles.forEach(title => {
            const span = title.querySelector('span');
            if (span) {
                span.classList.remove('font-medium');
                span.classList.remove('text-[#21484B]');
                span.classList.add('text-[#7A7A7A]');
            }
        });
        collapseItems.forEach(item => {
            item.classList.remove('flex');
            item.classList.add('hidden');
        });
        menuLinks.forEach(link => {
            link.classList.remove('text-[#21484B]');
            link.classList.add('text-[#7A7A7A]');
        });
        if (this.checked) {
            const currentTitle = menuTitles[index].querySelector('span');
            if (currentTitle) {
                currentTitle.classList.add('font-medium');
                currentTitle.classList.remove('text-[#7A7A7A]');
                currentTitle.classList.add('text-[#21484B]');
            }
            collapseItems[index].classList.remove('hidden');
            collapseItems[index].classList.add('flex');
        }
    });
});

// Mobile language dropdown
function initializeMobileLanguageDropdown() {
    const mobileLangDropdown = document.querySelector('[data-mobile-lang-dropdown]');
    const mobileLangButton = document.querySelector('[data-mobile-lang-button]');
    const dropdownContent = mobileLangDropdown.querySelector('[data-dropdown-content]');

    if (!mobileLangButton || !dropdownContent) return;

    function openDropdown() {
        dropdownContent.classList.remove('opacity-0', 'invisible', 'pointer-events-none');
        dropdownContent.classList.add('opacity-100', 'visible', 'pointer-events-auto');
    }

    function closeDropdown() {
        dropdownContent.classList.remove('opacity-100', 'visible', 'pointer-events-auto');
        dropdownContent.classList.add('opacity-0', 'invisible', 'pointer-events-none');
    }

    mobileLangButton.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        const isOpen = dropdownContent.classList.contains('opacity-100');
        if (isOpen) {
            closeDropdown();
        } else {
            openDropdown();
        }
    });

    document.addEventListener('click', function(e) {
        if (!mobileLangDropdown.contains(e.target)) {
            closeDropdown();
        }
    });

    const langOptions = dropdownContent.querySelectorAll('[data-lang-option]');
    const langText = mobileLangButton.querySelector('[data-lang-text]');

    langOptions.forEach(option => {
        option.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            if (langText) {
                langText.textContent = this.textContent;
            }

            closeDropdown();
        });
    });
}

// Initialize mobile language dropdown
initializeMobileLanguageDropdown();