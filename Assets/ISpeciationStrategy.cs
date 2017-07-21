using System.Collections.Generic;

public interface ISpeciationStrategy
{
    /// <summary>
    /// Speciates the genomes in genomeList into the number of species specified by specieCount
    /// and returns a newly constructed list of Specie objects containing the speciated genomes.
    /// </summary>
    IList<Specie> InitializeSpeciation(IList<NEATGenome> genomeList, int specieCount);

    /// <summary>
    /// Speciates the genomes in genomeList into the provided species. It is assumed that
    /// the genomeList represents all of the required genomes and that the species are currently empty.
    /// 
    /// This method can be used for initialization or completely respeciating an existing genome population.
    /// </summary>
    void SpeciateGenomes(IList<NEATGenome> genomeList, IList<Specie> specieList);

    /// <summary>
    /// Speciates the offspring genomes in genomeList into the provided species. In contrast to
    /// SpeciateGenomes() genomeList is taken to be a list of new genomes (e.g. offspring) that should be 
    /// added to existing species. That is, the specieList contain genomes that are not in genomeList
    /// that we wish to keep; typically these would be elite genomes that are the parents of the
    /// offspring.
    /// </summary>
    void SpeciateOffspring(IList<NEATGenome> genomeList, IList<Specie> specieList);
}