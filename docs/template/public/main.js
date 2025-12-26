export default {
    iconLinks: [
        {
            icon: 'github',
            href: 'https://github.com/JerrettDavis/ExperimentFramework',
            title: 'GitHub'
        },
        {
            icon: 'box-seam',
            href: 'https://www.nuget.org/packages?q=ExperimentFramework',
            title: 'NuGet'
        }
    ],
    start: () => {
        // Add smooth scrolling for anchor links
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const targetId = this.getAttribute('href');
                if (targetId === '#') return;

                const target = document.querySelector(targetId);
                if (target) {
                    e.preventDefault();
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });

        // Add copy button to code blocks
        document.querySelectorAll('pre code').forEach((block) => {
            const wrapper = block.closest('pre');
            if (!wrapper) return;

            const button = document.createElement('button');
            button.className = 'copy-btn';
            button.innerHTML = '<i class="bi bi-clipboard"></i>';
            button.title = 'Copy to clipboard';
            button.style.cssText = `
                position: absolute;
                top: 8px;
                right: 8px;
                padding: 4px 8px;
                background: var(--ef-surface-hover);
                border: 1px solid var(--ef-surface-border);
                border-radius: 4px;
                cursor: pointer;
                opacity: 0;
                transition: opacity 0.2s;
                font-size: 14px;
                color: var(--ef-text-secondary);
            `;

            wrapper.style.position = 'relative';
            wrapper.appendChild(button);

            wrapper.addEventListener('mouseenter', () => button.style.opacity = '1');
            wrapper.addEventListener('mouseleave', () => button.style.opacity = '0');

            button.addEventListener('click', async () => {
                try {
                    await navigator.clipboard.writeText(block.textContent);
                    button.innerHTML = '<i class="bi bi-check"></i>';
                    button.style.color = 'var(--ef-accent)';
                    setTimeout(() => {
                        button.innerHTML = '<i class="bi bi-clipboard"></i>';
                        button.style.color = 'var(--ef-text-secondary)';
                    }, 2000);
                } catch (err) {
                    console.error('Failed to copy:', err);
                }
            });
        });

        // Add reading progress indicator
        const progressBar = document.createElement('div');
        progressBar.id = 'reading-progress';
        progressBar.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 0%;
            height: 3px;
            background: linear-gradient(90deg, var(--ef-primary) 0%, var(--ef-secondary) 100%);
            z-index: 9999;
            transition: width 0.1s;
        `;
        document.body.appendChild(progressBar);

        window.addEventListener('scroll', () => {
            const scrollHeight = document.documentElement.scrollHeight - window.innerHeight;
            const scrolled = (window.scrollY / scrollHeight) * 100;
            progressBar.style.width = `${Math.min(scrolled, 100)}%`;
        });

        // Enhance external links
        document.querySelectorAll('article a[href^="http"]').forEach(link => {
            if (!link.href.includes(window.location.hostname)) {
                link.setAttribute('target', '_blank');
                link.setAttribute('rel', 'noopener noreferrer');
                if (!link.querySelector('.bi-box-arrow-up-right')) {
                    link.innerHTML += ' <i class="bi bi-box-arrow-up-right" style="font-size: 0.75em; opacity: 0.7;"></i>';
                }
            }
        });

        // Add active section highlighting in TOC
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const id = entry.target.getAttribute('id');
                    if (id) {
                        document.querySelectorAll('.toc .nav-link').forEach(link => {
                            link.classList.remove('active');
                            if (link.getAttribute('href') === `#${id}`) {
                                link.classList.add('active');
                            }
                        });
                    }
                }
            });
        }, { threshold: 0.5, rootMargin: '-100px 0px -66% 0px' });

        document.querySelectorAll('article h2[id], article h3[id]').forEach(heading => {
            observer.observe(heading);
        });

        console.log('%c ExperimentFramework Docs ',
            'background: linear-gradient(135deg, #6366f1, #8b5cf6, #a855f7); color: white; padding: 10px 20px; border-radius: 8px; font-weight: bold; font-size: 14px;');
    }
}
