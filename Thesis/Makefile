# makefile pro preklad LaTeX verze Bc. prace
# makefile for compilation of the thesis
# (c) 2008 Michal Bidlo
# E-mail: bidlom AT fit vutbr cz
# Edited by: dytrych AT fit vutbr cz
#===========================================
# asi budete chtit prejmenovat / you will probably rename:
CO=project

all: $(CO).pdf

pdf: $(CO).pdf

$(CO).ps: $(CO).dvi
	dvips $(CO)

$(CO).pdf: clean
	pdflatex $(CO)
	-bibtex $(CO)
	pdflatex $(CO)
	pdflatex $(CO)

$(CO).dvi: $(CO).tex $(CO).bib
	latex $(CO)
	-bibtex $(CO)
	latex $(CO)
	latex $(CO)

clean:
	rm -f *.dvi *.log $(CO).blg $(CO).bbl $(CO).toc *.aux $(CO).out $(CO).lof $(CO).ptc
	rm -f $(CO).pdf
	rm -f *~

pack:
	tar czvf $(CO).tar.gz *.tex *.bib *.bst ./template-fig/* ./bib-styles/* ./cls/* zadani.pdf $(CO).pdf Makefile Changelog

# Spocita normostrany / Count of standard pages
normostrany:
	echo "scale=2; `detex -n chapters.tex | sed s/"^ *"/""/ | sed s/"^	*"/""/ | wc -c`/1800;" | bc

